using System.Collections.Immutable;
using System.Linq;
using AdoGen.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AdoGen.Generator.Pipelines;

internal static class Discovery
{
    private const string SqlServerLib = "AdoGen.SqlServer";
    private const string PostgreSqlLib = "AdoGen.PostgreSql";

    private const string SqlServerResultInterface = $"{SqlServerLib}.ISqlResult";
    private const string SqlServerDomainInterface = $"{SqlServerLib}.ISqlDomainModel";
    private const string SqlServerBulkInterface = $"{SqlServerLib}.ISqlBulkModel";
    private const string SqlServerProfile = "SqlProfile";

    private const string PostgreResultInterface = $"{PostgreSqlLib}.INpgsqlResult";
    private const string PostgreDomainInterface = $"{PostgreSqlLib}.INpgsqlDomainModel";
    private const string PostgreBulkInterface = $"{PostgreSqlLib}.INpgsqlBulkModel";
    private const string PostgreProfile = "NpgsqlProfile";

    public static IncrementalValuesProvider<DiscoveryDto> DiscoverDtos(IncrementalGeneratorInitializationContext context)
        => FilterTypes(
            context,
            CreateDtoCandidates(context),
            BuildProfilesIndex(FindProfiles(context)));
    
    private static IncrementalValuesProvider<INamedTypeSymbol> CreateDtoCandidates(IncrementalGeneratorInitializationContext context)
        => context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                static (ctx, ct) => ctx.SemanticModel.GetDeclaredSymbol((TypeDeclarationSyntax)ctx.Node, ct) as INamedTypeSymbol)
            .Where(static x => x is not null)
            .Where(static x => x!.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
            .Where(static x => !x!.IsStatic)
            .Select(static (x, _) => x!)
            .WithComparer(SymbolEqualityComparer.Default);
    
    private static IncrementalValuesProvider<DiscoveryDto> FilterTypes(
        IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<INamedTypeSymbol> candidates,
        IncrementalValueProvider<ImmutableArray<(INamedTypeSymbol Dto, INamedTypeSymbol Profile, SemanticModel Model, SqlProviderKind Provider)>> profilesIndex) =>
        candidates
            .Collect()
            .Combine(profilesIndex)
            .Combine(context.CompilationProvider)
            .SelectMany(static (input, ct) =>
            {
                var ((types, profiles), compilation) = input;

                // Deduplicate: partial types appear once per syntax declaration.
                var distinctTypes = types.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default);

                var ssResult = compilation.GetTypeByMetadataName(SqlServerResultInterface);
                var ssDomain = compilation.GetTypeByMetadataName(SqlServerDomainInterface);
                var ssBulk = compilation.GetTypeByMetadataName(SqlServerBulkInterface);

                var pgResult = compilation.GetTypeByMetadataName(PostgreResultInterface);
                var pgDomain = compilation.GetTypeByMetadataName(PostgreDomainInterface);
                var pgBulk = compilation.GetTypeByMetadataName(PostgreBulkInterface);

                if (ssResult is null || ssDomain is null || ssBulk is null)
                {
                    // SqlServer abstractions not referenced => no generation.
                    // If Postgres is referenced, we still want to generate for it.
                    // We'll keep going and just not match ss kinds.
                }

                if (pgResult is null || pgDomain is null || pgBulk is null)
                {
                    // Same logic as above.
                }

                var builder = ImmutableArray.CreateBuilder<DiscoveryDto>(types.Length);

                foreach (var typeSymbol in distinctTypes)
                {
                    // Check SQL Server
                    var ssKind = SqlModelKind.None;
                    if (ssResult is not null && ssDomain is not null && ssBulk is not null)
                    {
                        ssKind =
                            typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, ssBulk)) ? SqlModelKind.Bulk :
                            typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, ssDomain)) ? SqlModelKind.Domain :
                            typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, ssResult)) ? SqlModelKind.Result :
                            SqlModelKind.None;
                    }

                    if (ssKind != SqlModelKind.None)
                    {
                        INamedTypeSymbol? ssProfile = null;
                        SemanticModel? ssModel = null;

                        for (var i = 0; i < profiles.Length; i++)
                        {
                            if (profiles[i].Provider != SqlProviderKind.SqlServer) continue;
                            if (!SymbolEqualityComparer.Default.Equals(profiles[i].Dto, typeSymbol)) continue;

                            ssProfile = profiles[i].Profile;
                            ssModel = profiles[i].Model;
                            break;
                        }

                        builder.Add(new DiscoveryDto(typeSymbol, ssKind, ssProfile, ssModel, SqlProviderKind.SqlServer));
                    }

                    // Check PostgreSQL (independently, so a type can target both providers)
                    var pgKind = SqlModelKind.None;
                    if (pgResult is not null && pgDomain is not null && pgBulk is not null)
                    {
                        pgKind =
                            typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, pgBulk)) ? SqlModelKind.Bulk :
                            typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, pgDomain)) ? SqlModelKind.Domain :
                            typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, pgResult)) ? SqlModelKind.Result :
                            SqlModelKind.None;
                    }

                    if (pgKind != SqlModelKind.None)
                    {
                        INamedTypeSymbol? pgProfile = null;
                        SemanticModel? pgModel = null;

                        for (var i = 0; i < profiles.Length; i++)
                        {
                            if (profiles[i].Provider != SqlProviderKind.PostgreSql) continue;
                            if (!SymbolEqualityComparer.Default.Equals(profiles[i].Dto, typeSymbol)) continue;

                            pgProfile = profiles[i].Profile;
                            pgModel = profiles[i].Model;
                            break;
                        }

                        builder.Add(new DiscoveryDto(typeSymbol, pgKind, pgProfile, pgModel, SqlProviderKind.PostgreSql));
                    }
                }

                return builder.ToImmutable();
            });
    
    private static IncrementalValuesProvider<(INamedTypeSymbol Profile, SemanticModel Model, SqlProviderKind Provider)>
        FindProfiles(IncrementalGeneratorInitializationContext context)
        => context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, ct) =>
                {
                    if (ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node, ct) is not INamedTypeSymbol symbol)
                        return (symbol: (INamedTypeSymbol)null!, ctx.SemanticModel, Provider: default(SqlProviderKind));

                    var baseType = symbol.BaseType;
                    if (baseType is null || baseType.TypeArguments.Length != 1)
                        return (symbol: (INamedTypeSymbol)null!, ctx.SemanticModel, Provider: default(SqlProviderKind));

                    var ns = baseType.ContainingNamespace?.ToDisplayString();

                    if (baseType.Name == SqlServerProfile && ns == SqlServerLib)
                        return (symbol, ctx.SemanticModel, Provider: SqlProviderKind.SqlServer);

                    if (baseType.Name == PostgreProfile && ns == PostgreSqlLib)
                        return (symbol, ctx.SemanticModel, Provider: SqlProviderKind.PostgreSql);

                    return (symbol: (INamedTypeSymbol)null!, ctx.SemanticModel, Provider: default(SqlProviderKind));
                })
            .Where(static x => x.symbol is not null)
            .Select(static (x, _) => (x.symbol!, x.SemanticModel, x.Provider));

    private static IncrementalValueProvider<ImmutableArray<(INamedTypeSymbol Dto, INamedTypeSymbol Profile, SemanticModel Model, SqlProviderKind Provider)>>
        BuildProfilesIndex(IncrementalValuesProvider<(INamedTypeSymbol Profile, SemanticModel Model, SqlProviderKind Provider)> profiles)
        => profiles
            .Select(static (p, _) => (Dto: (INamedTypeSymbol)p.Profile.BaseType!.TypeArguments[0], p.Profile, p.Model, p.Provider))
            .Collect();
}
    
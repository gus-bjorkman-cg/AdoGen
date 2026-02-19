using System.Collections.Immutable;
using System.Linq;
using AdoGen.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AdoGen.Generator.Pipelines;

internal static class Discovery
{
    private const string AbstractionsLib = "AdoGen.Abstractions";
    private const string SqlResultInterface = $"{AbstractionsLib}.ISqlResult";
    private const string SqlDomainInterface = $"{AbstractionsLib}.ISqlDomainModel";
    private const string SqlBulkInterface = $"{AbstractionsLib}.ISqlBulkModel";
    private const string SqlProfile = nameof(SqlProfile);
    
    public static IncrementalValuesProvider<DiscoveryDto> DiscoverDtos(
        IncrementalGeneratorInitializationContext context)
        => FilterTypes(
            context,
            CreateDtoCandidates(context),
            BuildProfilesIndex(FindSqlProfiles(context)));
    
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
        IncrementalValueProvider<ImmutableArray<(INamedTypeSymbol Dto, INamedTypeSymbol Profile, SemanticModel Model)>> profilesIndex) =>
        candidates
            .Collect()
            .Combine(profilesIndex)
            .Combine(context.CompilationProvider)
            .SelectMany(static (input, ct) =>
            {
                var ((types, profiles), compilation) = input;

                var sqlResultInterface = compilation.GetTypeByMetadataName(SqlResultInterface);
                var sqlDomainInterface = compilation.GetTypeByMetadataName(SqlDomainInterface);
                var sqlBulkInterface = compilation.GetTypeByMetadataName(SqlBulkInterface);

                if (sqlResultInterface is null || sqlDomainInterface is null || sqlBulkInterface is null)
                    return ImmutableArray<DiscoveryDto>.Empty;
                
                var builder = ImmutableArray.CreateBuilder<DiscoveryDto>(types.Length);

                foreach (var typeSymbol in types)
                {
                    var kind =
                        typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, sqlBulkInterface)) ? SqlModelKind.Bulk :
                        typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, sqlDomainInterface)) ? SqlModelKind.Domain :
                        typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, sqlResultInterface)) ? SqlModelKind.Result :
                        SqlModelKind.None;

                    if (kind == SqlModelKind.None) continue;

                    INamedTypeSymbol? profile = null;
                    SemanticModel? model = null;
                    
                    for (var i = 0; i < profiles.Length; i++)
                    {
                        if (!SymbolEqualityComparer.Default.Equals(profiles[i].Dto, typeSymbol))
                            continue;

                        profile = profiles[i].Profile;
                        model = profiles[i].Model;
                        break;
                    }

                    builder.Add(new DiscoveryDto(typeSymbol, kind, profile, model));
                }
                
                return builder.ToImmutable();
            });
    
    private static IncrementalValuesProvider<(INamedTypeSymbol Profile, SemanticModel Model)>
        FindSqlProfiles(IncrementalGeneratorInitializationContext context)
        => context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, ct) =>
                {
                    if (ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node, ct) is not INamedTypeSymbol symbol) 
                        return (null!, ctx.SemanticModel);

                    var baseType = symbol.BaseType;
                    
                    if (baseType is null || 
                        baseType.Name != SqlProfile || 
                        baseType.TypeArguments.Length != 1 || 
                        baseType.ContainingNamespace?.ToDisplayString() != AbstractionsLib) 
                        return (null!, ctx.SemanticModel);

                    return (symbol, ctx.SemanticModel);
                })
            .Where(static x => x.symbol is not null)
            .Select(static (x, _) => (x.symbol!, x.SemanticModel));

    private static IncrementalValueProvider<ImmutableArray<(INamedTypeSymbol Dto, INamedTypeSymbol Profile, SemanticModel Model)>>
        BuildProfilesIndex(IncrementalValuesProvider<(INamedTypeSymbol Profile, SemanticModel Model)> profiles)
        => profiles
            .Select(static (p, _) => (Dto: (INamedTypeSymbol)p.Profile.BaseType!.TypeArguments[0], p.Profile, p.Model))
            .Collect();
}

internal enum SqlModelKind : byte
{
    None = 0,
    Result = 1,
    Domain = 2,
    Bulk = 3
}

internal readonly record struct DiscoveryDto(
    INamedTypeSymbol Dto,
    SqlModelKind Kind,
    INamedTypeSymbol? Profile,
    SemanticModel? ProfileSemanticModel);
    
internal readonly record struct ValidatedDiscoveryDto(
    DiscoveryDto Discovery,
    ProfileInfo ProfileInfo,
    ImmutableArray<Diagnostic> Diagnostics);
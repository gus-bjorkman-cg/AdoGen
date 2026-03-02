using System.Collections.Generic;
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

    public static IncrementalValuesProvider<DiscoveryDto> DiscoverDtos(
        IncrementalGeneratorInitializationContext context)
        => FilterTypes(
            context,
            CreateDtoCandidates(context),
            BuildProfilesIndex(context));

    private static IncrementalValuesProvider<INamedTypeSymbol> CreateDtoCandidates(
        IncrementalGeneratorInitializationContext context)
        => context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                static (ctx, ct) =>
                    ctx.SemanticModel.GetDeclaredSymbol((TypeDeclarationSyntax)ctx.Node, ct) as INamedTypeSymbol)
            .Where(static x => x is not null)
            .Select(static (x, _) => x!)
            .Where(static x => x.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
            .Where(static x => !x.IsStatic)
            .WithComparer(SymbolEqualityComparer.Default);

    private static IncrementalValuesProvider<DiscoveryDto> FilterTypes(
        IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<INamedTypeSymbol> candidates,
        IncrementalValueProvider<ImmutableArray<DiscoveryModel>> profilesIndex) =>
        candidates
            .Collect()
            .Combine(profilesIndex)
            .Combine(context.CompilationProvider)
            .SelectMany(static (input, ct) =>
            {
                var ((types, profiles), compilation) = input;

                var distinctTypes = types.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                var builder = ImmutableArray.CreateBuilder<DiscoveryDto>(types.Length);
                var adoGenInterfaces = GetAdoGenInterfaces(compilation);

                foreach (var type in distinctTypes)
                {
                    var typeProfiles = profiles.Where(y => SymbolEqualityComparer.Default.Equals(y.Dto, type));
                    var typeDiscoveries = type.AllInterfaces
                        .Select(i => adoGenInterfaces.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(i, x.Interface)))
                        .Where(static x => x.Interface is not null)
                        .GroupBy(static x => x.Provider)
                        .OrderByDescending(static x => x.Key)
                        .Select(static x => new
                        {
                            Provider = x.Key, Kind = x.Select(y => y.Kind).FirstOrDefault(),
                            Interface = x.Select(y => y.Interface).FirstOrDefault()
                        })
                        .Where(x => typeProfiles.Any(y => y.Provider == x.Provider))
                        .Select(x =>
                        {
                            var profile = typeProfiles.First(y => y.Provider == x.Provider);
                            return new DiscoveryDto(type, x.Kind, profile.Profile, profile.Model, x.Provider);
                        });

                    builder.AddRange(typeDiscoveries);
                }

                return builder.ToImmutable();
            });

    private static IncrementalValueProvider<ImmutableArray<DiscoveryModel>> BuildProfilesIndex(
        IncrementalGeneratorInitializationContext context)
        => context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, ct) =>
                {
                    if (ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node, ct) is not
                        INamedTypeSymbol symbol)
                        return (symbol: null!, ctx.SemanticModel, Provider: SqlProviderKind.None);

                    var baseType = symbol.BaseType;
                    if (baseType is null || baseType.TypeArguments.Length != 1)
                        return (symbol, ctx.SemanticModel, Provider: SqlProviderKind.None);

                    var provider = baseType.Name switch
                    {
                        SqlServerProfile => SqlProviderKind.SqlServer,
                        PostgreProfile => SqlProviderKind.PostgreSql,
                        _ => SqlProviderKind.None
                    };

                    return (symbol, ctx.SemanticModel, Provider: provider);
                })
            .Where(static x => x.symbol is not null)
            .Where(static x => x.Provider is not SqlProviderKind.None)
            .Select(static (p, _) => new DiscoveryModel(
                (INamedTypeSymbol)p.symbol.BaseType!.TypeArguments[0],
                p.symbol, 
                p.SemanticModel, 
                p.Provider))
            .Collect();

    private static ImmutableArray<AdoGenInterfaceInfo> GetAdoGenInterfaces(Compilation compilation) =>
         AdoGenInterfaces
            .Select(x =>
            {
                var interfaceSymbol = compilation.GetTypeByMetadataName(x.@interface);
                return interfaceSymbol is null ? AdoGenInterfaceInfo.Empty : new AdoGenInterfaceInfo(interfaceSymbol, x.kind, x.provider);
            })
            .Where(static x => x != AdoGenInterfaceInfo.Empty)
            .ToImmutableArray();
    
    private static readonly List<(string @interface, SqlModelKind kind, SqlProviderKind provider)> AdoGenInterfaces =
    [
        (SqlServerResultInterface, SqlModelKind.Result, SqlProviderKind.SqlServer),
        (SqlServerDomainInterface, SqlModelKind.Domain, SqlProviderKind.SqlServer),
        (SqlServerBulkInterface, SqlModelKind.Bulk, SqlProviderKind.SqlServer),
        (PostgreResultInterface, SqlModelKind.Result, SqlProviderKind.PostgreSql),
        (PostgreDomainInterface, SqlModelKind.Domain, SqlProviderKind.PostgreSql),
        (PostgreBulkInterface, SqlModelKind.Bulk, SqlProviderKind.PostgreSql)
    ];

    private readonly record struct AdoGenInterfaceInfo(
        INamedTypeSymbol Interface,
        SqlModelKind Kind,
        SqlProviderKind Provider)
    {
        public static AdoGenInterfaceInfo Empty => new(null!, SqlModelKind.None, SqlProviderKind.None);
    }

    private readonly record struct DiscoveryModel(
        INamedTypeSymbol Dto,
        INamedTypeSymbol Profile,
        SemanticModel Model,
        SqlProviderKind Provider);
}
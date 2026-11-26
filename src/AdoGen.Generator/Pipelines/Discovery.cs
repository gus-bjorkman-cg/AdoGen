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
    private const string NpgsqlSqlLib = "AdoGen.PostgreSql";

    private const string SqlServerMapperInterface = $"{SqlServerLib}.ISqlMapper";
    private const string SqlServerDomainInterface = $"{SqlServerLib}.ISqlDomainModel";
    private const string SqlServerBulkInterface = $"{SqlServerLib}.ISqlBulkModel";
    private const string SqlServerProfile = "SqlProfile";

    private const string NpgsqlMapperInterface = $"{NpgsqlSqlLib}.INpgsqlMapper";
    private const string NpgsqlDomainInterface = $"{NpgsqlSqlLib}.INpgsqlDomainModel";
    private const string NpgsqlBulkInterface = $"{NpgsqlSqlLib}.INpgsqlBulkModel";
    private const string NpgsqlProfile = "NpgsqlProfile";

    private const string SqlServerNamingAttribute = $"{SqlServerLib}.SqlNamingAttribute";
    private const string NpgsqlNamingAttribute = $"{NpgsqlSqlLib}.NpgsqlNamingAttribute";
    
    public static IncrementalValuesProvider<DiscoveryDto> DiscoverDtos(
        IncrementalGeneratorInitializationContext context)
        => FilterTypes(
            context,
            CreateDtoCandidates(context),
            BuildProfilesIndex(context));

    private static IncrementalValueProvider<ImmutableArray<INamedTypeSymbol>> CreateDtoCandidates(
        IncrementalGeneratorInitializationContext context)
        => context.SyntaxProvider.CreateSyntaxProvider(
                // A DTO (class, record, struct or record struct) needs a base list to implement a marker interface; at least one partial part declares it.
                static (node, _) => node is ClassDeclarationSyntax { BaseList: not null } or RecordDeclarationSyntax { BaseList: not null } or StructDeclarationSyntax { BaseList: not null },
                static (ctx, ct) =>
                    ctx.SemanticModel.GetDeclaredSymbol((TypeDeclarationSyntax)ctx.Node, ct) as INamedTypeSymbol)
            .Where(static x => x is not null)
            .Select(static (x, _) => x!)
            .WithComparer(SymbolEqualityComparer.Default)
            .Collect();

    private static IncrementalValuesProvider<DiscoveryDto> FilterTypes(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<ImmutableArray<INamedTypeSymbol>> candidates,
        IncrementalValueProvider<ImmutableArray<DiscoveryModel>> profilesIndex) =>
        candidates
            .Combine(profilesIndex)
            .Combine(context.CompilationProvider)
            .SelectMany(static (input, ct) =>
            {
                var ((types, profiles), compilation) = input;

                var distinctTypes = types.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                var builder = ImmutableArray.CreateBuilder<DiscoveryDto>(types.Length);
                var adoGenInterfaces = GetAdoGenInterfaces(compilation);
                var pluralizeSqlServer = ShouldPluralize(compilation, SqlServerNamingAttribute);
                var pluralizeNpgsql = ShouldPluralize(compilation, NpgsqlNamingAttribute);

                foreach (var type in distinctTypes)
                {
                    var typeProfiles = profiles.Where(y => SymbolEqualityComparer.Default.Equals(y.Dto, type)).ToImmutableArray();
                    // Only markers declared on the type itself (directly or via a user interface) count.
                    // Markers inherited from a base class belong to the base DTO, not to the derived type.
                    var typeDiscoveries = GetOwnInterfaces(type)
                        .Select(i => adoGenInterfaces.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(i, x.Interface)))
                        .Where(static x => x.Interface is not null)
                        .GroupBy(static x => x.Provider)
                        .OrderByDescending(static x => x.Key)
                        .Select(static x => new
                        {
                            Provider = x.Key, Kind = x.Max(y => y.Kind)
                        })
                        .ToArray();

                    // Determine which provider (the lowest-value one with Domain-or-higher) owns the shared {Dto}Patch class
                    var patchOwner = typeDiscoveries
                        .Where(x => x.Kind >= SqlModelKind.Domain)
                        .OrderBy(x => (int)x.Provider)
                        .Select(x => (SqlProviderKind?)x.Provider)
                        .FirstOrDefault();

                    foreach (var typeDiscovery in typeDiscoveries)
                    {
                        var discoveryModel = DiscoveryModel.Empty;
                        var profileCount = 0;
                        foreach (var typeProfile in typeProfiles.Where(typeProfile => typeProfile.Provider == typeDiscovery.Provider))
                        {
                            if (profileCount++ == 0) discoveryModel = typeProfile;
                        }

                        builder.Add(new DiscoveryDto(type, typeDiscovery.Kind, discoveryModel.Profile, discoveryModel.Model, typeDiscovery.Provider,
                            ShouldGeneratePatchClass: typeDiscovery.Kind >= SqlModelKind.Domain && typeDiscovery.Provider == patchOwner,
                            ProfileCount: profileCount,
                            PluralizeTableNames: typeDiscovery.Provider is SqlProviderKind.PostgreSql ? pluralizeNpgsql : pluralizeSqlServer));
                    }
                }

                return builder.ToImmutable();
            });

    /// <summary>
    /// Reads <c>Pluralize</c> from the provider's assembly-level naming attribute; defaults to true when absent.
    /// </summary>
    private static bool ShouldPluralize(Compilation compilation, string attributeMetadataName)
    {
        var attributeType = compilation.GetTypeByMetadataName(attributeMetadataName);
        if (attributeType is null) return true;

        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType)) continue;

            foreach (var named in attribute.NamedArguments)
            {
                if (named.Key == "Pluralize" && named.Value.Value is bool pluralize) return pluralize;
            }
        }

        return true;
    }

    private static IEnumerable<INamedTypeSymbol> GetOwnInterfaces(INamedTypeSymbol type)
    {
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var direct in type.Interfaces)
        {
            if (seen.Add(direct)) yield return direct;
            foreach (var inherited in direct.AllInterfaces)
            {
                if (seen.Add(inherited)) yield return inherited;
            }
        }
    }

    private static IncrementalValueProvider<ImmutableArray<DiscoveryModel>> BuildProfilesIndex(
        IncrementalGeneratorInitializationContext context)
        => context.SyntaxProvider.CreateSyntaxProvider(
                // Profiles derive from a generic SqlProfile<T> / NpgsqlProfile<T> base class.
                static (node, _) => node is ClassDeclarationSyntax { BaseList.Types: [{ Type: GenericNameSyntax or QualifiedNameSyntax { Right: GenericNameSyntax } or AliasQualifiedNameSyntax { Name: GenericNameSyntax } }, ..] },
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
                        NpgsqlProfile => SqlProviderKind.PostgreSql,
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
        (SqlServerMapperInterface, SqlModelKind.Mapper, SqlProviderKind.SqlServer),
        (SqlServerDomainInterface, SqlModelKind.Domain, SqlProviderKind.SqlServer),
        (SqlServerBulkInterface, SqlModelKind.Bulk, SqlProviderKind.SqlServer),
        (NpgsqlMapperInterface, SqlModelKind.Mapper, SqlProviderKind.PostgreSql),
        (NpgsqlDomainInterface, SqlModelKind.Domain, SqlProviderKind.PostgreSql),
        (NpgsqlBulkInterface, SqlModelKind.Bulk, SqlProviderKind.PostgreSql)
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
        SqlProviderKind Provider)
    {
        public static DiscoveryModel Empty => new(null!, null!, null!, SqlProviderKind.None);
    }
}
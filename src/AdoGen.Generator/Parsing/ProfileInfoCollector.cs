using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using AdoGen.Generator.Diagnostics;
using AdoGen.Generator.Emitters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;

namespace AdoGen.Generator.Parsing;

internal static class ProfileInfoCollector
{
    private const string RuleFor = nameof(RuleFor);

    private static readonly List<ISqlTypeLiterals> SqlTypeLiterals =
    [
        SqlTypeLiteralsSqlServer.Instance, SqlTypeLiteralsPostgreSql.Instance
    ];
    
    internal static ProfileInfo Resolve(
        DiscoveryDto discoveryDto, 
        ImmutableArray<Diagnostic>.Builder diagnostics,
        ImmutableArray<IPropertySymbol> props,
        CancellationToken ct)
    {
        var (dto, _, profile, model, provider) = discoveryDto;
        var collected = Collect(profile!, dto, model!, diagnostics, provider, props, ct);

        if (collected.Keys.IsDefaultOrEmpty || collected.Keys.Length == 0)
        {
            var location = profile!.Locations.FirstOrDefault()
                           ?? dto.Locations.FirstOrDefault()
                           ?? Location.None;

            diagnostics.Add(Diagnostic.Create(
                SqlDiagnostics.MissingKey,
                location, dto.Name));
        }

        return collected;
    }
    
    private static ProfileInfo Collect(
        INamedTypeSymbol profileSymbol,
        INamedTypeSymbol dtoType,
        SemanticModel model,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        SqlProviderKind provider,
        ImmutableArray<IPropertySymbol> props,
        CancellationToken ct)
    {
        var configs = new Dictionary<string, ParamConfig>(StringComparer.Ordinal);
        var identityKeys = new HashSet<string>(StringComparer.Ordinal);
        var keys = new List<string>();
        
        var dtoProps = props.ToImmutableDictionary(p => p.Name, p => p, StringComparer.Ordinal);
        var schema = provider.DefaultSchema();
        var table = dtoType.Name.PluralizeSimple();
        var expressionSyntaxes = profileSymbol.GetProfileExpressions();

        foreach (var expressionSyntax in expressionSyntaxes)
        {
            if (expressionSyntax.Expression is not IdentifierNameSyntax id) continue;

            switch (id.Identifier.Text)
            {
                case "Table":
                    if (expressionSyntax.ArgumentList.Arguments is { Count: 1 } al &&
                        model.TryGetConstString(al[0].Expression, default, out var tn) && !string.IsNullOrWhiteSpace(tn))
                        table = tn!;
                    break;

                case "Schema":
                    if (expressionSyntax.ArgumentList.Arguments is { Count: 1 } asl &&
                        model.TryGetConstString(asl[0].Expression, default, out var sc) && !string.IsNullOrWhiteSpace(sc))
                        schema = sc!;
                    break;

                case "Key":
                case "Identity":
                    if (expressionSyntax.ArgumentList.Arguments is { Count: 1 } kal &&
                        kal[0].Expression is LambdaExpressionSyntax lambda)
                    {
                        var propName = lambda.TryGetPropertyNameFromLambdaStrict(model);
                        if (propName != null && dtoProps.ContainsKey(propName))
                        {
                            switch (id.Identifier.Text)
                            {
                                case "Key" when !keys.Contains(propName, StringComparer.Ordinal):
                                    keys.Add(propName);
                                    break;
                                case "Identity":
                                    identityKeys.Add(propName);
                                    break;
                            }
                        }
                    }
                    break;
                case RuleFor:
                    ConfigureChainParser.ParseConfigureRootAndForwardChain(
                        model, 
                        dtoType, 
                        dtoProps, 
                        expressionSyntax, 
                        configs,
                        provider,
                        diagnostics, 
                        ct);
                    break;
            }
        }
        
        if (keys.Count == 0)
        {
            var idProp = dtoProps.Keys.FirstOrDefault(n => string.Equals(n, "Id", StringComparison.OrdinalIgnoreCase));
            if (idProp is not null) keys.Add(idProp);
        }

        // Ensure configs exist for all props (conventions)
        foreach (var prop in dtoProps.Values)
        {
            if (!configs.ContainsKey(prop.Name))
            {
                configs[prop.Name] = new ParamConfig
                {
                    PropertyName = prop.Name,
                    PropertyType = prop.Type,
                    ParameterName = prop.Name,
                    DbType = prop.MapDefaultDbType(provider)
                };
            }
            else if (configs[prop.Name].DbType is null)
            {
                var config = configs[prop.Name];
                config.DbType = prop.MapDefaultDbType(provider);
            }
        }
        
        foreach (var cfg in configs.Values)
            if (cfg.SqlTypeLiteral is "") 
                cfg.SqlTypeLiteral = SqlTypeLiterals.First(x => x.IsMatch(cfg)).Get(cfg);
        
        return new ProfileInfo(
            Schema: schema,
            Table: table,
            Keys: [.. keys],
            IdentityKeys: identityKeys.ToImmutableHashSet(StringComparer.Ordinal),
            DtoProperties: props,
            ParamsByProperty: configs.ToImmutableDictionary(StringComparer.Ordinal),
            Namespace: dtoType.GetNamespace()
        );
    }
    
    private static string DefaultSchema(this SqlProviderKind provider) =>
        provider switch
        {
            SqlProviderKind.SqlServer => "dbo",
            SqlProviderKind.PostgreSql => "public",
            _ => throw new NotSupportedException($"Unsupported provider: {provider}")
        };
    
    private static DbTypeRef MapDefaultDbType(this IPropertySymbol propertySymbol, SqlProviderKind provider) =>
        provider switch
        {
            SqlProviderKind.SqlServer => propertySymbol.Type.MapDefaultSqlDbType(),
            SqlProviderKind.PostgreSql => propertySymbol.Type.MapDefaultNpgsqlDbType(),
            _ => throw new NotSupportedException($"Unsupported provider: {provider}")
        };
    
    extension(INamedTypeSymbol profileSymbol)
    {
        private ImmutableArray<InvocationExpressionSyntax> GetProfileExpressions() =>
            profileSymbol.DeclaringSyntaxReferences
                .Select(x => x.GetSyntax())
                .OfType<ClassDeclarationSyntax>()
                .SelectMany(x => x.Members.OfType<ConstructorDeclarationSyntax>())
                .SelectMany(x =>
                {
                    var nodes = new List<SyntaxNode>();
                    if (x.Body is { } body) nodes.AddRange(body.DescendantNodes());
                    if (x.ExpressionBody is { } exprBody) nodes.AddRange(exprBody.DescendantNodes());
                    return nodes;
                })
                .OfType<InvocationExpressionSyntax>()
                .Where(x => x.Expression is IdentifierNameSyntax)
                .ToImmutableArray();

        private string GetNamespace() =>
            profileSymbol.ContainingNamespace.IsGlobalNamespace
                ? "GlobalNamespace"
                : profileSymbol.ContainingNamespace.ToDisplayString();
    }
}

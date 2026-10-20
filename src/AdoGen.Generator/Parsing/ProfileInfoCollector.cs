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
        var (dto, _, profile, model, provider, _, _) = discoveryDto;
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

    /// <summary>
    /// Builds a ProfileInfo for a mapper-only DTO (ISqlMapper/INpgsqlMapper only, no Domain/Bulk
    /// interface) that has no SqlProfile/NpgsqlProfile. Only Map()/parameter-factory generation is
    /// needed in this path, so Table/Schema/Keys are irrelevant and no profile syntax is parsed —
    /// every property gets a default DbType convention. Properties requiring mandatory config
    /// (string/decimal/byte[]) simply end up with a ParamConfig lacking Size/Precision/Scale; the
    /// mapper emitters skip generating a CreateParameter* factory for those specific properties.
    /// </summary>
    internal static ProfileInfo ResolveMapperOnly(
        DiscoveryDto discoveryDto,
        ImmutableArray<IPropertySymbol> props)
    {
        var provider = discoveryDto.Provider;
        var configs = new Dictionary<string, ParamConfig>(StringComparer.Ordinal);

        foreach (var prop in props)
        {
            configs[prop.Name] = new ParamConfig
            {
                PropertyName = prop.Name,
                PropertyType = prop.Type,
                ParameterName = prop.Name,
                DbType = prop.MapDefaultDbType(provider)
            };
        }

        foreach (var cfg in configs.Values)
            if (cfg.SqlTypeLiteral is "")
                cfg.SqlTypeLiteral = SqlTypeLiterals.First(x => x.IsMatch(cfg)).Get(cfg);

        return new ProfileInfo(
            Schema: string.Empty,
            Table: string.Empty,
            Keys: ImmutableArray<string>.Empty,
            IdentityKeys: ImmutableHashSet<string>.Empty,
            DtoProperties: props,
            ParamsByProperty: configs.ToImmutableDictionary(StringComparer.Ordinal),
            Namespace: discoveryDto.Dto.GetNamespace()
        );
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
        
        var dtoProps = props.ToImmutableDictionary(static p => p.Name, p => p, StringComparer.Ordinal);
        var schema = provider.DefaultSchema();
        var table = dtoType.Name.PluralizeSimple();
        var expressionSyntaxes = profileSymbol.GetProfileExpressions();

        foreach (var expressionSyntax in expressionSyntaxes)
        {
            var methodName = expressionSyntax.GetProfileMethodName();
            if (methodName is null) continue;

            switch (methodName)
            {
                case "Table":
                    if (expressionSyntax.ArgumentList.Arguments is { Count: 1 } al &&
                        model.TryGetConstString(al[0].Expression, ct, out var tn) && !string.IsNullOrWhiteSpace(tn))
                        table = tn!;
                    else
                        diagnostics.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, expressionSyntax.GetLocation(), dtoType.Name, methodName));
                    break;

                case "Schema":
                    if (expressionSyntax.ArgumentList.Arguments is { Count: 1 } asl &&
                        model.TryGetConstString(asl[0].Expression, ct, out var sc) && !string.IsNullOrWhiteSpace(sc))
                        schema = sc!;
                    else
                        diagnostics.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, expressionSyntax.GetLocation(), dtoType.Name, methodName));
                    break;

                case "Key":
                case "Identity":
                    var propName = TryGetSelectedProperty(expressionSyntax, model, dtoProps);
                    if (propName is null)
                    {
                        diagnostics.Add(Diagnostic.Create(SqlDiagnostics.UnsupportedProfileExpression, expressionSyntax.GetLocation(), dtoType.Name, methodName));
                        break;
                    }
                    
                    switch (methodName)
                    {
                        case "Key":
                            keys.Add(propName);
                            break;
                        case "Identity":
                            identityKeys.Add(propName);
                            break;
                    }
                    break;
                case RuleFor:
                    if (TryGetSelectedProperty(expressionSyntax, model, dtoProps) is null)
                    {
                        diagnostics.Add(Diagnostic.Create(SqlDiagnostics.UnsupportedProfileExpression, expressionSyntax.GetLocation(), dtoType.Name, methodName));
                        break;
                    }

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
            var idProp = dtoProps.Keys.FirstOrDefault(static n => string.Equals(n, "Id", StringComparison.OrdinalIgnoreCase));
            if (idProp is not null) keys.Add(idProp);
        }

        // Ensure configs exist for all props (conventions)
        foreach (var prop in dtoProps.Values)
        {
            if (!configs.TryGetValue(prop.Name, out var config))
            {
                configs[prop.Name] = new ParamConfig
                {
                    PropertyName = prop.Name,
                    PropertyType = prop.Type,
                    ParameterName = prop.Name,
                    DbType = prop.MapDefaultDbType(provider)
                };
            }
            else if (config.DbType is null)
            {
                config.DbType = prop.MapDefaultDbType(provider);
            }
        }
        
        foreach (var cfg in configs.Values)
            if (cfg.SqlTypeLiteral is "") 
                cfg.SqlTypeLiteral = SqlTypeLiterals.First(x => x.IsMatch(cfg)).Get(cfg);
        
        return new ProfileInfo(
            Schema: schema,
            Table: table,
            Keys: keys.Distinct().ToImmutableArray(),
            IdentityKeys: identityKeys.Distinct().ToImmutableHashSet(StringComparer.Ordinal),
            DtoProperties: props,
            ParamsByProperty: configs.ToImmutableDictionary(StringComparer.Ordinal),
            Namespace: dtoType.GetNamespace()
        );
    }
    
    /// <summary>
    /// Resolves the property selected by a single inline-lambda argument (<c>x => x.Prop</c>), or null when
    /// the argument has another shape (method group, variable, nested member, unmapped property).
    /// </summary>
    internal static string? TryGetSelectedProperty(
        InvocationExpressionSyntax invocation,
        SemanticModel model,
        IReadOnlyDictionary<string, IPropertySymbol> dtoProps)
    {
        if (invocation.ArgumentList.Arguments is not { Count: 1 } args) return null;
        if (args[0].Expression is not LambdaExpressionSyntax lambda) return null;

        var propName = lambda.TryGetPropertyNameFromLambdaStrict(model);
        return propName is not null && dtoProps.ContainsKey(propName) ? propName : null;
    }

    /// <summary>
    /// Returns the profile method name for <c>Method(...)</c> and <c>this.Method(...)</c> calls, otherwise null
    /// (chained calls such as <c>.VarChar(50)</c> are reached through their root <c>RuleFor</c>).
    /// </summary>
    private static string? GetProfileMethodName(this InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: IdentifierNameSyntax name } => name.Identifier.Text,
            _ => null
        };

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
                .Select(static x => x.GetSyntax())
                .OfType<ClassDeclarationSyntax>()
                .SelectMany(static x => x.Members.OfType<ConstructorDeclarationSyntax>())
                .SelectMany(static x =>
                {
                    var nodes = new List<SyntaxNode>();
                    if (x.Body is { } body) nodes.AddRange(body.DescendantNodes());
                    if (x.ExpressionBody is { } exprBody) nodes.AddRange(exprBody.DescendantNodes());
                    return nodes;
                })
                .OfType<InvocationExpressionSyntax>()
                .Where(static x => x.GetProfileMethodName() is not null)
                .ToImmutableArray();

        private string GetNamespace() =>
            profileSymbol.ContainingNamespace.IsGlobalNamespace
                ? "GlobalNamespace"
                : profileSymbol.ContainingNamespace.ToDisplayString();
    }
}

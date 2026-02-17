using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;

namespace AdoGen.Generator.Parsing;

internal static class ProfileInfoCollector
{
    private const string RuleFor = nameof(RuleFor);
    
    public static ProfileInfo Collect(
        INamedTypeSymbol profileSymbol,
        INamedTypeSymbol dtoType,
        SemanticModel model,
        SourceProductionContext spc)
    {
        var dtoProps = dtoType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
            .ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);

        var configs = new Dictionary<string, ParamConfig>(StringComparer.Ordinal);
        string? schema = null;
        string? table = null;
        var keys = new List<string>();
        var identityKeys = new HashSet<string>(StringComparer.Ordinal);
        
        foreach (var syntaxRef in profileSymbol.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is not ClassDeclarationSyntax cls) continue;

            foreach (var ctor in cls.Members.OfType<ConstructorDeclarationSyntax>())
            {
                IEnumerable<SyntaxNode> nodes = [];
                if (ctor.Body is { } body) nodes = nodes.Concat(body.DescendantNodes());
                if (ctor.ExpressionBody is { } exprBody) nodes = nodes.Concat(exprBody.DescendantNodes());

                foreach (var inv in nodes.OfType<InvocationExpressionSyntax>())
                {
                    if (inv.Expression is IdentifierNameSyntax id)
                    {
                        switch (id.Identifier.Text)
                        {
                            case "Table":
                                if (inv.ArgumentList.Arguments is { Count: 1 } al &&
                                    model.TryGetConstString(al[0].Expression, default, out var tn) && !string.IsNullOrWhiteSpace(tn))
                                    table = tn!;
                                break;

                            case "Schema":
                                if (inv.ArgumentList.Arguments is { Count: 1 } asl &&
                                    model.TryGetConstString(asl[0].Expression, default, out var sc) && !string.IsNullOrWhiteSpace(sc))
                                    schema = sc!;
                                break;

                            case "Key":
                            case "Identity":
                                if (inv.ArgumentList.Arguments is { Count: 1 } kal &&
                                    kal[0].Expression is LambdaExpressionSyntax lambda)
                                {
                                    var propName = lambda.TryGetPropertyNameFromLambdaStrict(model);
                                    if (propName is { } pn && dtoProps.ContainsKey(pn))
                                    {
                                        if (id.Identifier.Text == "Key" && !keys.Contains(pn, StringComparer.Ordinal)) keys.Add(pn);
                                        if (id.Identifier.Text == "Identity") identityKeys.Add(pn);
                                    }
                                }
                                break;
                        }
                    }

                    // Property chains starting with RuleFor(...)
                    var isConfigureCall =
                        (inv.Expression is IdentifierNameSyntax cid && cid.Identifier.Text == RuleFor) ||
                        (inv.Expression is MemberAccessExpressionSyntax mae && mae.Name.Identifier.Text == RuleFor);

                    if (isConfigureCall)
                    {
                        ConfigureChainParser.ParseConfigureRootAndForwardChain(spc, model, dtoType, dtoProps, inv, configs);
                    }
                }
            }
        }

        // Defaults
        schema ??= "dbo";
        table ??= dtoType.Name.PluralizeSimple();

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
                    DbType = prop.Type.MapDefaultSqlDbType()   
                };
            }
            else if (configs[prop.Name].DbType is null)
            {
                var config = configs[prop.Name];
                config.DbType = config.PropertyType.MapDefaultSqlDbType();
            }
        }

        return new ProfileInfo(
            Schema: schema,
            Table: table,
            Keys: [.. keys],
            IdentityKeys: identityKeys.ToImmutableHashSet(StringComparer.Ordinal),
            ParamsByProperty: configs.ToImmutableDictionary(StringComparer.Ordinal)
        );
    }
}

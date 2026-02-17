using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AdoGen.Generator.Diagnostics;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;
using AdoGen.Generator.Parsing;
using AdoGen.Generator.Pipelines;

namespace AdoGen.Generator.Emitters;

internal static class SqlParameterHelpersEmitter
{
    private const string RuleFor = nameof(RuleFor);
    
    public static void Emit(SourceProductionContext spc, DiscoveryDto discoveryDto)
    {
        var (dto, kind, profileSymbol, model) = discoveryDto;
        
        if (kind == SqlModelKind.None) return;
        
        var baseType = profileSymbol.BaseType!;
        var dtoType = (INamedTypeSymbol)baseType.TypeArguments[0];

        var dtoProps = dtoType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
            .ToDictionary(p => p.Name, p => p);

        var configs = new Dictionary<string, ParamConfig>(StringComparer.Ordinal);

        // Gather from constructors (block or expression-bodied)
        foreach (var syntaxRef in profileSymbol.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is not ClassDeclarationSyntax cls) continue;

            foreach (var ctor in cls.Members.OfType<ConstructorDeclarationSyntax>())
            {
                IEnumerable<SyntaxNode> nodes = Array.Empty<SyntaxNode>();
                if (ctor.Body is { } body) nodes = nodes.Concat(body.DescendantNodes());
                if (ctor.ExpressionBody is { } exprBody) nodes = nodes.Concat(exprBody.DescendantNodes());

                foreach (var inv in nodes.OfType<InvocationExpressionSyntax>())
                {
                    var isConfigureCall =
                        (inv.Expression is IdentifierNameSyntax id && id.Identifier.Text == RuleFor) ||
                        (inv.Expression is MemberAccessExpressionSyntax mae && mae.Name.Identifier.Text == RuleFor);
                    if (!isConfigureCall) continue;

                    if (inv.ArgumentList.Arguments.Count != 1 ||
                        inv.ArgumentList.Arguments[0].Expression is not LambdaExpressionSyntax)
                        continue;

                    ConfigureChainParser.ParseConfigureRootAndForwardChain(spc, model, dtoType, dtoProps, inv, configs);
                }
            }
        }

        // Enforce required configs
        bool hasErrors = false;
        foreach (var prop in dtoProps.Values)
        {
            var name = prop.Name;
            var t = prop.Type;

            if (t.IsString())
            {
                if (!configs.TryGetValue(name, out var cfg) || cfg.DbType is null || cfg.Size is null)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.StringMissing, profileSymbol.Locations.FirstOrDefault() ?? Location.None, dtoType.Name, name));
                    hasErrors = true;
                }
            }
            else if (t.IsDecimal())
            {
                if (!configs.TryGetValue(name, out var cfg) || cfg.DbType != SqlDbType.Decimal || cfg.Precision is null || cfg.Scale is null)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.DecimalMissing, profileSymbol.Locations.FirstOrDefault() ?? Location.None, dtoType.Name, name));
                    hasErrors = true;
                }
            }
            else if (t.IsByteArray())
            {
                if (!configs.TryGetValue(name, out var cfg) || cfg.DbType is null || cfg.Size is null)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.BinaryMissing, profileSymbol.Locations.FirstOrDefault() ?? Location.None, dtoType.Name, name));
                    hasErrors = true;
                }
            }
            else
            {
                if (!configs.TryGetValue(name, out var cfg))
                {
                    configs[name] = new ParamConfig
                    {
                        PropertyName = name,
                        PropertyType = t, 
                        ParameterName = name,
                        DbType = t.MapDefaultSqlDbType()
                    };
                }
                else if (cfg.DbType is null)
                {
                    var config = configs[name]; 
                    config.DbType = config.PropertyType.MapDefaultSqlDbType();
                }
            }
        }

        if (hasErrors) return;

        var constBuilder = new StringBuilder();
        var methodBuilder = new StringBuilder();

        foreach (var kvp in configs)
        {
            var cfg = kvp.Value!;
            constBuilder.AppendLine(CreateSqlParameterConst(cfg));
            methodBuilder.AppendLine(CreateSqlParameterMethod(cfg));
            methodBuilder.AppendLine();
        }
        
        var methods = methodBuilder.ToString().TrimEnd();

        var src = $$"""
            // <auto-generated />
            #nullable enable
            using System;
            using System.Data;
            using System.Runtime.CompilerServices;
            using Microsoft.Data.SqlClient;

            namespace AdoGen.Abstractions;

            /// <summary>
            /// Helper methods for creating typed SQL parameters for {{dtoType.Name}}.
            /// </summary>
            public static class {{dtoType.Name}}Sql
            {
            {{constBuilder}}
            {{methods}}
            }
            """;

        spc.AddSource($"{dtoType.Name}Sql.g.cs", src);
    }

    private static readonly SymbolDisplayFormat TypeDisplay = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
                                  SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private static string CreateSqlParameterConst(ParamConfig cfg)
        => $"""    public const string Parameter{cfg.PropertyName} = "{cfg.ParameterName}";""";

    private static string CreateSqlParameterMethod(ParamConfig cfg)
    {
        var methodName = $"CreateParameter{cfg.PropertyName}";
        var constName = $"Parameter{cfg.PropertyName}";
        var typeName = cfg.PropertyType.ToDisplayString(TypeDisplay);

        var isNullableValueType = cfg.PropertyType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };
        var valueExpr = cfg.PropertyType.IsReferenceType || isNullableValueType ? "value is null ? DBNull.Value : value" : "value";

        var optional = new StringBuilder();
        if (cfg.Size is { } size) optional.AppendLine($"        Size = {size},");
        if (cfg.Precision is { } pr) optional.AppendLine($"        Precision = {pr},");
        if (cfg.Scale is { } sc) optional.AppendLine($"        Scale = {sc},");

        var optionalParams = optional.ToString().TrimEnd();

        return $$"""
                /// <summary>
                /// Creates a SqlParameter with the configured db type and size/precision/scale.
                /// </summary>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static SqlParameter {{methodName}}({{typeName}} value, string? propertyName = null) => new()
                {
                    ParameterName = propertyName ?? {{constName}},
                    SqlDbType = SqlDbType.{{cfg.DbType}},
                    Value = {{valueExpr}},{{(string.IsNullOrEmpty(optionalParams) ? "" : "\n" + optionalParams)}}
                };
            """;
    }
}

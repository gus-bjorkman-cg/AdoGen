using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AdoGen.Generator.Diagnostics;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;

namespace AdoGen.Generator.Parsing;

internal static class ConfigureChainParser
{
    public static void ParseConfigureRootAndForwardChain(
        SemanticModel model,
        INamedTypeSymbol dtoType,
        IReadOnlyDictionary<string, IPropertySymbol> dtoProps,
        InvocationExpressionSyntax configureInvocation,
        Dictionary<string, ParamConfig> configs,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder,
        CancellationToken ct)
    {
        var lambda = (LambdaExpressionSyntax)configureInvocation.ArgumentList.Arguments[0].Expression;
        var propName = lambda.TryGetPropertyNameFromLambdaStrict(model);

        if (propName is null || !dtoProps.TryGetValue(propName, out var propSymbol)) return;

        // determine provider based on containing profile base type namespace
        var provider = SqlProviderKind.SqlServer;
        if (configureInvocation.FirstAncestorOrSelf<ClassDeclarationSyntax>() is { } cls)
        {
            var profileSymbol = model.GetDeclaredSymbol(cls, ct) as INamedTypeSymbol;
            var bt = profileSymbol?.BaseType;
            var ns = bt?.ContainingNamespace?.ToDisplayString();
            if (ns == "AdoGen.PostgreSql") provider = SqlProviderKind.PostgreSql;
        }

        var cfg = new ParamConfig
        {
            PropertyName = propName,
            PropertyType = propSymbol.Type,
            ParameterName = propName
        };

        var chainMethods = new List<ChainMethod>();
        var current = configureInvocation.Parent;

        while (current is MemberAccessExpressionSyntax nextMae)
        {
            if (nextMae.Parent is InvocationExpressionSyntax nextCall)
            {
                chainMethods.Add(new ChainMethod(nextMae.Name.Identifier.Text, nextCall.ArgumentList.Arguments, nextCall));
                current = nextCall.Parent;
            }
            else break;
        }

        foreach (var (methodName, args, node) in chainMethods)
        {
            switch (methodName)
            {
                case "Type":
                    if (args.Count == 1)
                    {
                        if (provider == SqlProviderKind.SqlServer)
                        {
                            if (model.TryGetConstEnumArg<SqlDbType>(args[0].Expression, ct, out var sqlDbt))
                                cfg.DbType = DbTypeRef.SqlServer(sqlDbt.ToString());
                            else
                                diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                        }
                        else
                        {
                            if (model.TryGetConstEnumMember(args[0].Expression, ct, out var enumMember))
                                cfg.DbType = DbTypeRef.PostgreSql(enumMember);
                            else
                                diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                        }
                    }
                    else
                        diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "Size":
                    if (args.Count == 1 && model.TryGetConstInt(args[0].Expression, ct, out var size))
                        cfg.Size = size;
                    else
                        diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "Precision":
                    if (args.Count == 1 && model.TryGetConstInt(args[0].Expression, ct, out var prec))
                        cfg.Precision = prec;
                    else
                        diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "Scale":
                    if (args.Count == 1 && model.TryGetConstInt(args[0].Expression, ct, out var sc))
                        cfg.Scale = sc;
                    else
                        diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "Name":
                    if (args.Count == 1 && model.TryGetConstString(args[0].Expression, ct, out var pname) && !string.IsNullOrWhiteSpace(pname))
                        cfg.ParameterName = pname!;
                    else
                        diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                // SQL Server shorthands
                case "NVarChar" when provider == SqlProviderKind.SqlServer:
                    if (args.Count == 1 && model.TryGetConstInt(args[0].Expression, ct, out var nsize))
                    {
                        cfg.DbType = DbTypeRef.SqlServer(SqlDbType.NVarChar.ToString());
                        cfg.Size = nsize;
                    }
                    else
                        diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "VarChar" when provider == SqlProviderKind.SqlServer:
                    if (args.Count == 1 && model.TryGetConstInt(args[0].Expression, ct, out var vsize))
                    {
                        cfg.DbType = DbTypeRef.SqlServer(SqlDbType.VarChar.ToString());
                        cfg.Size = vsize;
                    }
                    else
                        diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "NChar" when provider == SqlProviderKind.SqlServer:
                    if (args.Count == 1 && model.TryGetConstInt(args[0].Expression, ct, out var ncsize))
                    {
                        cfg.DbType = DbTypeRef.SqlServer(SqlDbType.NChar.ToString());
                        cfg.Size = ncsize;
                    }
                    else
                        diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "Char" when provider == SqlProviderKind.SqlServer:
                    if (args.Count == 1 && model.TryGetConstInt(args[0].Expression, ct, out var csize))
                    {
                        cfg.DbType = DbTypeRef.SqlServer(SqlDbType.Char.ToString());
                        cfg.Size = csize;
                    }
                    else
                        diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "VarBinary" when provider == SqlProviderKind.SqlServer:
                    if (args.Count == 1 && model.TryGetConstInt(args[0].Expression, ct, out var bsize))
                    {
                        cfg.DbType = DbTypeRef.SqlServer(SqlDbType.VarBinary.ToString());
                        cfg.Size = bsize;
                    }
                    else
                        diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "Decimal" when provider == SqlProviderKind.SqlServer:
                    if (args.Count == 2
                        && model.TryGetConstInt(args[0].Expression, ct, out var precision)
                        && model.TryGetConstInt(args[1].Expression, ct, out var scale))
                    {
                        cfg.DbType = DbTypeRef.SqlServer(SqlDbType.Decimal.ToString());
                        cfg.Precision = precision;
                        cfg.Scale = scale;
                    }
                    else
                        diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                // PostgreSQL shorthands
                case "Varchar" when provider == SqlProviderKind.PostgreSql:
                    if (args.Count == 1 && model.TryGetConstInt(args[0].Expression, ct, out var psize))
                    {
                        cfg.DbType = DbTypeRef.PostgreSql("Varchar");
                        cfg.Size = psize;
                    }
                    else
                        diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "Text" when provider == SqlProviderKind.PostgreSql:
                    if (args.Count == 0)
                    {
                        cfg.DbType = DbTypeRef.PostgreSql("Text");
                    }
                    else
                        diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "Bytea" when provider == SqlProviderKind.PostgreSql:
                    if (args.Count == 0)
                    {
                        cfg.DbType = DbTypeRef.PostgreSql("Bytea");
                    }
                    else
                        diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "Decimal" when provider == SqlProviderKind.PostgreSql:
                    if (args.Count == 2
                        && model.TryGetConstInt(args[0].Expression, ct, out var pprecision)
                        && model.TryGetConstInt(args[1].Expression, ct, out var pscale))
                    {
                        cfg.DbType = DbTypeRef.PostgreSql("Numeric");
                        cfg.Precision = pprecision;
                        cfg.Scale = pscale;
                    }
                    else
                        diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "Nullable":
                    if (args.Count == 0) cfg.IsNullable = true;
                    else diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "NotNull":
                    if (args.Count == 0) cfg.IsNullable = false;
                    else diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "DefaultValue":
                    if (args.Count == 1 && model.TryGetConstString(args[0].Expression, ct, out var expr) && !string.IsNullOrWhiteSpace(expr))
                        cfg.DefaultSqlExpression = expr!;
                    else
                        diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                default:
                    break;
            }
        }

        configs[cfg.PropertyName] = cfg;
    }
}

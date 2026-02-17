using System.Collections.Generic;
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
        SourceProductionContext spc,
        SemanticModel model,
        INamedTypeSymbol dtoType,
        IReadOnlyDictionary<string, IPropertySymbol> dtoProps,
        InvocationExpressionSyntax configureInvocation,
        Dictionary<string, ParamConfig> configs)
    {
        var lambda = (LambdaExpressionSyntax)configureInvocation.ArgumentList.Arguments[0].Expression;
        var propName = lambda.TryGetPropertyNameFromLambdaStrict(model);
        
        if (propName is null || !dtoProps.TryGetValue(propName, out var propSymbol)) return;

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
                    if (args.Count == 1 && model.TryGetConstEnumArg<SqlDbType>(args[0].Expression, CancellationToken.None, out var dbt))
                        cfg.DbType = dbt;
                    else
                        spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "Size":
                    if (args.Count == 1 && model.TryGetConstInt(args[0].Expression, CancellationToken.None, out var size))
                        cfg.Size = size;
                    else
                        spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "Precision":
                    if (args.Count == 1 && model.TryGetConstInt(args[0].Expression, CancellationToken.None, out var prec))
                        cfg.Precision = prec;
                    else
                        spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "Scale":
                    if (args.Count == 1 && model.TryGetConstInt(args[0].Expression, CancellationToken.None, out var sc))
                        cfg.Scale = sc;
                    else
                        spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "Name":
                    if (args.Count == 1 && model.TryGetConstString(args[0].Expression, CancellationToken.None, out var pname) && !string.IsNullOrWhiteSpace(pname))
                        cfg.ParameterName = pname!;
                    else
                        spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "NVarChar":
                    if (args.Count == 1 && model.TryGetConstInt(args[0].Expression, CancellationToken.None, out var nsize))
                    {
                        cfg.DbType = SqlDbType.NVarChar;
                        cfg.Size = nsize;
                    }
                    else
                        spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "VarChar":
                    if (args.Count == 1 && model.TryGetConstInt(args[0].Expression, CancellationToken.None, out var vsize))
                    {
                        cfg.DbType = SqlDbType.VarChar;
                        cfg.Size = vsize;
                    }
                    else
                        spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "NChar":
                    if (args.Count == 1 && model.TryGetConstInt(args[0].Expression, CancellationToken.None, out var ncsize))
                    {
                        cfg.DbType = SqlDbType.NChar;
                        cfg.Size = ncsize;
                    }
                    else
                        spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "Char":
                    if (args.Count == 1 && model.TryGetConstInt(args[0].Expression, CancellationToken.None, out var csize))
                    {
                        cfg.DbType = SqlDbType.Char;
                        cfg.Size = csize;
                    }
                    else
                        spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "VarBinary":
                    if (args.Count == 1 && model.TryGetConstInt(args[0].Expression, CancellationToken.None, out var bsize))
                    { 
                        cfg.DbType = SqlDbType.VarBinary;
                        cfg.Size = bsize;
                    }
                    else
                        spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;
                
                case "Nullable":
                    if (args.Count == 0) cfg.IsNullable = true;
                    else
                        spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "NotNull":
                    if (args.Count == 0) cfg.IsNullable = false;
                    else
                        spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                case "DefaultValue":
                    if (args.Count == 1 && model.TryGetConstString(args[0].Expression, CancellationToken.None, out var expr) && !string.IsNullOrWhiteSpace(expr))
                        cfg.DefaultSqlExpression = expr!;
                    else
                        spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.NonConstantArg, node.GetLocation(), dtoType.Name, propName));
                    break;

                default:
                    break;
            }
        }

        configs[cfg.PropertyName] = cfg;
    }
}

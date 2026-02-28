using System.Collections.Immutable;
using System.Data;
using System.Threading;
using AdoGen.Generator.Diagnostics;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Parsing.SqlServer;

internal sealed class TypeChainHandlerSqlServer : IChainMethodHandler
{
    private TypeChainHandlerSqlServer() { }
    public static TypeChainHandlerSqlServer Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) =>
        provider is SqlProviderKind.SqlServer && methodName == "Type";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstEnumArg<SqlDbType>(chain.Args[0].Expression, ct, out var sqlDbt))
            cfg.DbType = DbTypeRef.SqlServer(sqlDbt.ToString());
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class NVarCharChainHandlerSqlServer : IChainMethodHandler
{
    private NVarCharChainHandlerSqlServer() { }
    public static NVarCharChainHandlerSqlServer Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) =>
        provider is SqlProviderKind.SqlServer && methodName == "NVarChar";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstInt(chain.Args[0].Expression, ct, out var size))
        {
            cfg.DbType = DbTypeRef.SqlServer(SqlDbType.NVarChar.ToString());
            cfg.Size = size;
        }
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class VarCharChainHandlerSqlServer : IChainMethodHandler
{
    private VarCharChainHandlerSqlServer() { }
    public static VarCharChainHandlerSqlServer Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) =>
        provider is SqlProviderKind.SqlServer && methodName == "VarChar";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstInt(chain.Args[0].Expression, ct, out var size))
        {
            cfg.DbType = DbTypeRef.SqlServer(SqlDbType.VarChar.ToString());
            cfg.Size = size;
        }
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class NCharChainHandlerSqlServer : IChainMethodHandler
{
    private NCharChainHandlerSqlServer() { }
    public static NCharChainHandlerSqlServer Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) =>
        provider is SqlProviderKind.SqlServer && methodName == "NChar";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstInt(chain.Args[0].Expression, ct, out var size))
        {
            cfg.DbType = DbTypeRef.SqlServer(SqlDbType.NChar.ToString());
            cfg.Size = size;
        }
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class CharChainHandlerSqlServer : IChainMethodHandler
{
    private CharChainHandlerSqlServer() { }
    public static CharChainHandlerSqlServer Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) =>
        provider is SqlProviderKind.SqlServer && methodName == "Char";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstInt(chain.Args[0].Expression, ct, out var size))
        {
            cfg.DbType = DbTypeRef.SqlServer(SqlDbType.Char.ToString());
            cfg.Size = size;
        }
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class VarBinaryChainHandlerSqlServer : IChainMethodHandler
{
    private VarBinaryChainHandlerSqlServer() { }
    public static VarBinaryChainHandlerSqlServer Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) =>
        provider is SqlProviderKind.SqlServer && methodName == "VarBinary";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstInt(chain.Args[0].Expression, ct, out var size))
        {
            cfg.DbType = DbTypeRef.SqlServer(SqlDbType.VarBinary.ToString());
            cfg.Size = size;
        }
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class DecimalChainHandlerSqlServer : IChainMethodHandler
{
    private DecimalChainHandlerSqlServer() { }
    public static DecimalChainHandlerSqlServer Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) =>
        provider is SqlProviderKind.SqlServer && methodName == "Decimal";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 2
            && model.TryGetConstInt(chain.Args[0].Expression, ct, out var precision)
            && model.TryGetConstInt(chain.Args[1].Expression, ct, out var scale))
        {
            cfg.DbType = DbTypeRef.SqlServer(SqlDbType.Decimal.ToString());
            cfg.Precision = precision;
            cfg.Scale = scale;
        }
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

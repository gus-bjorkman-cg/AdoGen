using System.Collections.Immutable;
using System.Data;
using System.Threading;
using AdoGen.Generator.Diagnostics;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Parsing.PostgreSql;

internal sealed class TypeChainHandlerNpgsql : IChainMethodHandler
{
    private const string MethodName = "Type";
    private TypeChainHandlerNpgsql() { }
    public static TypeChainHandlerNpgsql Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) =>
        provider is SqlProviderKind.PostgreSql && methodName == MethodName;

    public void Handle(
        SemanticModel model, 
        INamedTypeSymbol dtoType, 
        string propertyName,
        ChainMethod chain, 
        ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, 
        CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstEnumMember(chain.Args[0].Expression, ct, out var enumMember))
            cfg.DbType = DbTypeRef.PostgreSql(enumMember);
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class VarcharChainHandlerNpgsql : IChainMethodHandler
{
    private const string MethodName = "Varchar";
    private static readonly DbTypeRef DbTypeRef = DbTypeRef.PostgreSql(MethodName);
    private VarcharChainHandlerNpgsql() { }
    public static VarcharChainHandlerNpgsql Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) =>
        provider is SqlProviderKind.PostgreSql && methodName == MethodName;

    public void Handle(
        SemanticModel model, 
        INamedTypeSymbol dtoType, 
        string propertyName,
        ChainMethod chain, 
        ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, 
        CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstInt(chain.Args[0].Expression, ct, out var size))
        {
            cfg.DbType = DbTypeRef;
            cfg.Size = size;
        }
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class TextChainHandlerNpgsql : IChainMethodHandler
{
    private const string MethodName = nameof(SqlDbType.Text);
    private static readonly DbTypeRef DbTypeRef = DbTypeRef.PostgreSql(MethodName);
    private TextChainHandlerNpgsql() { }
    public static TextChainHandlerNpgsql Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) =>
        provider is SqlProviderKind.PostgreSql && methodName == MethodName;

    public void Handle(
        SemanticModel model, 
        INamedTypeSymbol dtoType, 
        string propertyName,
        ChainMethod chain, 
        ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, 
        CancellationToken ct)
    {
        if (chain.Args.Count == 0)
            cfg.DbType = DbTypeRef;
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class ByteaChainHandlerNpgsql : IChainMethodHandler
{
    private const string MethodName = "Bytea";
    private static readonly DbTypeRef DbTypeRef = DbTypeRef.PostgreSql(MethodName);
    private ByteaChainHandlerNpgsql() { }
    public static ByteaChainHandlerNpgsql Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) =>
        provider is SqlProviderKind.PostgreSql && methodName == MethodName;

    public void Handle(
        SemanticModel model, 
        INamedTypeSymbol dtoType, 
        string propertyName,
        ChainMethod chain, 
        ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, 
        CancellationToken ct)
    {
        if (chain.Args.Count == 0)
            cfg.DbType = DbTypeRef;
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class DecimalChainHandlerNpgsql : IChainMethodHandler
{
    private const string MethodName = nameof(SqlDbType.Decimal);
    private static readonly DbTypeRef DbTypeRef = DbTypeRef.PostgreSql("Numeric");
    private DecimalChainHandlerNpgsql() { }
    public static DecimalChainHandlerNpgsql Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) =>
        provider is SqlProviderKind.PostgreSql && methodName == MethodName;

    public void Handle(
        SemanticModel model, 
        INamedTypeSymbol dtoType, 
        string propertyName,
        ChainMethod chain, 
        ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, 
        CancellationToken ct)
    {
        if (chain.Args.Count == 2
            && model.TryGetConstInt(chain.Args[0].Expression, ct, out var precision)
            && model.TryGetConstInt(chain.Args[1].Expression, ct, out var scale))
        {
            cfg.DbType = DbTypeRef;
            cfg.Precision = precision;
            cfg.Scale = scale;
        }
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

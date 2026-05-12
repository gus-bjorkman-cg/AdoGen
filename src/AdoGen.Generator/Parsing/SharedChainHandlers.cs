using System.Collections.Immutable;
using System.Threading;
using AdoGen.Generator.Diagnostics;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Parsing;

internal sealed class SizeChainHandler : IChainMethodHandler
{
    private const string MethodName = "Size";
    private SizeChainHandler() { }
    public static SizeChainHandler Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) => methodName == MethodName;

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
            cfg.Size = size;
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class PrecisionChainHandler : IChainMethodHandler
{
    private const string MethodName = "Precision";
    private PrecisionChainHandler() { }
    public static PrecisionChainHandler Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) => methodName == MethodName;

    public void Handle(
        SemanticModel model, 
        INamedTypeSymbol dtoType, 
        string propertyName,
        ChainMethod chain, 
        ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, 
        CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstInt(chain.Args[0].Expression, ct, out var prec))
            cfg.Precision = prec;
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class ScaleChainHandler : IChainMethodHandler
{
    private const string MethodName = "Scale";
    private ScaleChainHandler() { }
    public static ScaleChainHandler Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) => methodName == MethodName;

    public void Handle(
        SemanticModel model, 
        INamedTypeSymbol dtoType, 
        string propertyName,
        ChainMethod chain, 
        ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, 
        CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstInt(chain.Args[0].Expression, ct, out var sc))
            cfg.Scale = sc;
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class NameChainHandler : IChainMethodHandler
{
    private const string MethodName = "Name";
    private NameChainHandler() { }
    public static NameChainHandler Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) => methodName == MethodName;

    public void Handle(
        SemanticModel model, 
        INamedTypeSymbol dtoType, 
        string propertyName,
        ChainMethod chain, 
        ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, 
        CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstString(chain.Args[0].Expression, ct, out var pname) && !string.IsNullOrWhiteSpace(pname))
            cfg.ParameterName = pname!;
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class NullableChainHandler : IChainMethodHandler
{
    private const string MethodName = "Nullable";
    private NullableChainHandler() { }
    public static NullableChainHandler Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) => methodName == MethodName;

    public void Handle(
        SemanticModel model, 
        INamedTypeSymbol dtoType, 
        string propertyName,
        ChainMethod chain, 
        ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, 
        CancellationToken ct)
    {
        if (chain.Args.Count == 0) cfg.IsNullable = true;
        else diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class NotNullChainHandler : IChainMethodHandler
{
    private const string MethodName = "NotNull";
    private NotNullChainHandler() { }
    public static NotNullChainHandler Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) => methodName == MethodName;

    public void Handle(
        SemanticModel model, 
        INamedTypeSymbol dtoType, 
        string propertyName,
        ChainMethod chain, 
        ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, 
        CancellationToken ct)
    {
        if (chain.Args.Count == 0) cfg.IsNullable = false;
        else diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class ReadOnlyChainHandler : IChainMethodHandler
{
    private const string MethodName = "ReadOnly";
    private ReadOnlyChainHandler() { }
    public static ReadOnlyChainHandler Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) => methodName == MethodName;

    public void Handle(
        SemanticModel model,
        INamedTypeSymbol dtoType,
        string propertyName,
        ChainMethod chain,
        ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder,
        CancellationToken ct)
    {
        if (chain.Args.Count == 0) cfg.IsReadOnly = true;
        else diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class DefaultValueChainHandler : IChainMethodHandler
{
    private const string MethodName = "DefaultValue";
    private DefaultValueChainHandler() { }
    public static DefaultValueChainHandler Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) => methodName == MethodName;

    public void Handle(
        SemanticModel model, 
        INamedTypeSymbol dtoType, 
        string propertyName,
        ChainMethod chain, 
        ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, 
        CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstString(chain.Args[0].Expression, ct, out var expr) && !string.IsNullOrWhiteSpace(expr))
            cfg.DefaultSqlExpression = expr!;
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class ConcurrencyTokenChainHandler : IChainMethodHandler
{
    private const string MethodName = "ConcurrencyToken";
    private ConcurrencyTokenChainHandler() { }
    public static ConcurrencyTokenChainHandler Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) => methodName == MethodName;

    public void Handle(
        SemanticModel model,
        INamedTypeSymbol dtoType,
        string propertyName,
        ChainMethod chain,
        ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder,
        CancellationToken ct)
    {
        if (chain.Args.Count == 0) cfg.IsConcurrencyToken = true;
        else diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}


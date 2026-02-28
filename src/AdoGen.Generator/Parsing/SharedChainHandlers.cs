using System.Collections.Immutable;
using System.Threading;
using AdoGen.Generator.Diagnostics;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Parsing;

internal sealed class SizeChainHandler : IChainMethodHandler
{
    private SizeChainHandler() { }
    public static SizeChainHandler Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) => methodName == "Size";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstInt(chain.Args[0].Expression, ct, out var size))
            cfg.Size = size;
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class PrecisionChainHandler : IChainMethodHandler
{
    private PrecisionChainHandler() { }
    public static PrecisionChainHandler Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) => methodName == "Precision";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstInt(chain.Args[0].Expression, ct, out var prec))
            cfg.Precision = prec;
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class ScaleChainHandler : IChainMethodHandler
{
    private ScaleChainHandler() { }
    public static ScaleChainHandler Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) => methodName == "Scale";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstInt(chain.Args[0].Expression, ct, out var sc))
            cfg.Scale = sc;
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class NameChainHandler : IChainMethodHandler
{
    private NameChainHandler() { }
    public static NameChainHandler Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) => methodName == "Name";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstString(chain.Args[0].Expression, ct, out var pname) && !string.IsNullOrWhiteSpace(pname))
            cfg.ParameterName = pname!;
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class NullableChainHandler : IChainMethodHandler
{
    private NullableChainHandler() { }
    public static NullableChainHandler Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) => methodName == "Nullable";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 0) cfg.IsNullable = true;
        else diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class NotNullChainHandler : IChainMethodHandler
{
    private NotNullChainHandler() { }
    public static NotNullChainHandler Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) => methodName == "NotNull";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 0) cfg.IsNullable = false;
        else diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class DefaultValueChainHandler : IChainMethodHandler
{
    private DefaultValueChainHandler() { }
    public static DefaultValueChainHandler Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) => methodName == "DefaultValue";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstString(chain.Args[0].Expression, ct, out var expr) && !string.IsNullOrWhiteSpace(expr))
            cfg.DefaultSqlExpression = expr!;
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}


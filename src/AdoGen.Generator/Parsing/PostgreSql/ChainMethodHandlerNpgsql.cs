using System.Collections.Immutable;
using System.Threading;
using AdoGen.Generator.Diagnostics;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Parsing.PostgreSql;

internal sealed class TypeChainHandlerNpgsql : IChainMethodHandler
{
    private TypeChainHandlerNpgsql() { }
    public static TypeChainHandlerNpgsql Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) =>
        provider is SqlProviderKind.PostgreSql && methodName == "Type";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstEnumMember(chain.Args[0].Expression, ct, out var enumMember))
            cfg.DbType = DbTypeRef.PostgreSql(enumMember);
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class VarcharChainHandlerNpgsql : IChainMethodHandler
{
    private VarcharChainHandlerNpgsql() { }
    public static VarcharChainHandlerNpgsql Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) =>
        provider is SqlProviderKind.PostgreSql && methodName == "Varchar";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 1 && model.TryGetConstInt(chain.Args[0].Expression, ct, out var size))
        {
            cfg.DbType = DbTypeRef.PostgreSql("Varchar");
            cfg.Size = size;
        }
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class TextChainHandlerNpgsql : IChainMethodHandler
{
    private TextChainHandlerNpgsql() { }
    public static TextChainHandlerNpgsql Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) =>
        provider is SqlProviderKind.PostgreSql && methodName == "Text";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 0)
            cfg.DbType = DbTypeRef.PostgreSql("Text");
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class ByteaChainHandlerNpgsql : IChainMethodHandler
{
    private ByteaChainHandlerNpgsql() { }
    public static ByteaChainHandlerNpgsql Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) =>
        provider is SqlProviderKind.PostgreSql && methodName == "Bytea";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 0)
            cfg.DbType = DbTypeRef.PostgreSql("Bytea");
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

internal sealed class DecimalChainHandlerNpgsql : IChainMethodHandler
{
    private DecimalChainHandlerNpgsql() { }
    public static DecimalChainHandlerNpgsql Instance { get; } = new();

    public bool IsMatch(SqlProviderKind provider, string methodName) =>
        provider is SqlProviderKind.PostgreSql && methodName == "Decimal";

    public void Handle(
        SemanticModel model, INamedTypeSymbol dtoType, string propertyName,
        ChainMethod chain, ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder, CancellationToken ct)
    {
        if (chain.Args.Count == 2
            && model.TryGetConstInt(chain.Args[0].Expression, ct, out var precision)
            && model.TryGetConstInt(chain.Args[1].Expression, ct, out var scale))
        {
            cfg.DbType = DbTypeRef.PostgreSql("Numeric");
            cfg.Precision = precision;
            cfg.Scale = scale;
        }
        else
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

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

    private static bool IsValidIdentifier(string name)
    {
        if (name.Length == 0 || char.IsDigit(name[0])) return false;
        foreach (var c in name)
        {
            if (!(c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))) return false;
        }
        return true;
    }

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
        {
            // The column name doubles as the SQL parameter name and part of generated C# identifiers.
            if (IsValidIdentifier(pname!))
                cfg.ParameterName = pname!;
            else
                diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.InvalidColumnName, chain.Node.GetLocation(), dtoType.Name, propertyName, pname));
        }
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


internal sealed class IgnoreChainHandler : IChainMethodHandler
{
    private const string MethodName = "Ignore";
    private IgnoreChainHandler() { }
    public static IgnoreChainHandler Instance { get; } = new();

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
        if (chain.Args.Count == 0) cfg.IsIgnored = true;
        else diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.NonConstantArg, chain.Node.GetLocation(), dtoType.Name, propertyName));
    }
}

/// <summary>
/// <c>.AsString(size)</c>: stores a non-[Flags] enum as its member name in a VARCHAR(size) column.
/// </summary>
internal sealed class AsStringChainHandler : IChainMethodHandler
{
    private const string MethodName = "AsString";
    private readonly SqlProviderKind _provider;
    private readonly string _dbTypeMember;

    private AsStringChainHandler(SqlProviderKind provider, string dbTypeMember)
    {
        _provider = provider;
        _dbTypeMember = dbTypeMember;
    }

    public static AsStringChainHandler SqlServer { get; } = new(SqlProviderKind.SqlServer, "VarChar");
    public static AsStringChainHandler Npgsql { get; } = new(SqlProviderKind.PostgreSql, "Varchar");

    public bool IsMatch(SqlProviderKind provider, string methodName) => provider == _provider && methodName == MethodName;

    public void Handle(
        SemanticModel model,
        INamedTypeSymbol dtoType,
        string propertyName,
        ChainMethod chain,
        ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder,
        CancellationToken ct)
    {
        var (underlying, _) = cfg.PropertyType.UnwrapNullable();
        var isSupportedEnum = underlying.TypeKind == TypeKind.Enum && !underlying.IsFlagsEnum();

        if (!isSupportedEnum ||
            chain.Args.Count != 1 ||
            !model.TryGetConstInt(chain.Args[0].Expression, ct, out var size) ||
            size <= 0)
        {
            diagnosticsBuilder.Add(Diagnostic.Create(SqlDiagnostics.InvalidAsString, chain.Node.GetLocation(),
                dtoType.Name, propertyName, cfg.PropertyType.ToDisplayString()));
            return;
        }

        cfg.IsEnumAsString = true;
        cfg.DbType = _provider is SqlProviderKind.PostgreSql ? DbTypeRef.PostgreSql(_dbTypeMember) : DbTypeRef.SqlServer(_dbTypeMember);
        cfg.Size = size;
    }
}

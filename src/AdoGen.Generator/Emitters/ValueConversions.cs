using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Emitters;

/// <summary>
/// A compile-time value conversion between a CLR property type and the primitive type the provider reads/writes.
/// </summary>
/// <param name="WireType">C# keyword of the primitive written to the provider (e.g. <c>long</c>).</param>
/// <param name="Getter">Typed <c>DbDataReader</c> getter for the primitive (e.g. <c>GetInt64</c>).</param>
/// <param name="ClrType">Fully qualified non-nullable CLR type of the property.</param>
internal readonly record struct WireConversion(string WireType, string Getter, string ClrType)
{
    public string Write(string value) => $"({WireType}){value}";
    public string Read(string reader, string ordinal) => $"({ClrType}){reader}.{Getter}({ordinal})";
}

/// <summary>
/// Resolves conversions for CLR types the providers can't bind directly: enums stored as integers and
/// unsigned / <c>sbyte</c> integers (no native SQL type). The conversion is driven by the configured DbType so
/// that an explicit <c>.Type(...)</c> override reads/writes with the matching primitive.
/// </summary>
internal static class ValueConversions
{
    public static WireConversion? Resolve(ParamConfig cfg)
    {
        if (cfg.IsEnumAsString || cfg.DbType is not { } dbType) return null;

        var (underlying, _) = cfg.PropertyType.UnwrapNullable();
        var needsConversion = underlying.TypeKind == TypeKind.Enum
            || underlying.SpecialType is SpecialType.System_SByte or SpecialType.System_UInt16
                or SpecialType.System_UInt32 or SpecialType.System_UInt64;

        if (!needsConversion) return null;

        var wire = dbType.Provider switch
        {
            SqlProviderKind.SqlServer => dbType.EnumMember switch
            {
                "TinyInt" => ("byte", "GetByte"),
                "SmallInt" => ("short", "GetInt16"),
                "Int" => ("int", "GetInt32"),
                "BigInt" => ("long", "GetInt64"),
                "Decimal" => ("decimal", "GetDecimal"),
                _ => default((string, string)?)
            },
            SqlProviderKind.PostgreSql => dbType.EnumMember switch
            {
                "Smallint" => ("short", "GetInt16"),
                "Integer" => ("int", "GetInt32"),
                "Bigint" => ("long", "GetInt64"),
                "Numeric" => ("decimal", "GetDecimal"),
                _ => default((string, string)?)
            },
            _ => null
        };

        if (wire is not { } w) return null;

        return new WireConversion(w.Item1, w.Item2, underlying.ToDisplayString(RoslynSymbolExtensions.GetterKeyFormat));
    }

    /// <summary>
    /// Converted write expression for a non-null <paramref name="value"/>, or null when the value is bound as-is.
    /// <paramref name="factoryPrefix"/> qualifies the enum-string helpers (e.g. <c>UserSql.</c>; empty inside the factory class).
    /// </summary>
    public static string? Write(ParamConfig cfg, string value, string factoryPrefix)
    {
        if (cfg.IsEnumAsString) return $"{factoryPrefix}{EnumToDbMethod(cfg)}({value})";
        return Resolve(cfg) is { } conversion ? conversion.Write(value) : null;
    }

    /// <summary>
    /// Converted read expression for a non-null column, or null when the default typed getter applies.
    /// </summary>
    public static string? Read(ParamConfig cfg, string reader, string ordinal, string factoryPrefix)
    {
        if (cfg.IsEnumAsString) return $"{factoryPrefix}{EnumFromDbMethod(cfg)}({reader}.GetString({ordinal}))";
        return Resolve(cfg) is { } conversion ? conversion.Read(reader, ordinal) : null;
    }

    /// <summary>Name of the generated enum-to-string helper for <paramref name="cfg"/>.</summary>
    public static string EnumToDbMethod(ParamConfig cfg) => $"{cfg.PropertyName}ToDb";

    /// <summary>Name of the generated string-to-enum helper for <paramref name="cfg"/>.</summary>
    public static string EnumFromDbMethod(ParamConfig cfg) => $"{cfg.PropertyName}FromDb";
}

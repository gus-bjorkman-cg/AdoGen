namespace AdoGen.Generator.Models;

/// <summary>
/// Immutable per-column metadata computed once after validation and shared across all emitters.
/// Value equality is required for Roslyn incremental caching.
/// </summary>
internal readonly record struct ColumnInfo(
    string Name,               // CLR property name
    string ColumnNameQuoted,   // [Name] or "Name"
    string ParameterName,      // the unquoted parameter/column name
    string SqlType,            // INT, NVARCHAR(100), …
    string PropertyType,       // C# type literal e.g. "int", "string?"
    bool IsNullable,
    bool IsIdentity,
    bool IsKey,
    string? DefaultSqlExpression,
    ColumnRole Role
);


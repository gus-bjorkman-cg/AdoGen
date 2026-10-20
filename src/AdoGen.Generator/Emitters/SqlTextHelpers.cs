using System;
using AdoGen.Generator.Models;

namespace AdoGen.Generator.Emitters;

/// <summary>
/// Dialect-neutral SQL text fragments shared by the provider SQL text builders.
/// </summary>
internal static class SqlTextHelpers
{
    private const string DefaultKeyword = "DEFAULT ";

    /// <summary>
    /// Returns <c>" DEFAULT {expr}"</c> for a column default, or an empty string.
    /// A leading <c>DEFAULT</c> keyword in the profile value is accepted for backward compatibility.
    /// </summary>
    public static string DefaultClause(string? expression)
    {
        if (expression is null) return "";

        var expr = expression.Trim();
        if (expr.StartsWith(DefaultKeyword, StringComparison.OrdinalIgnoreCase))
            expr = expr.Substring(DefaultKeyword.Length).TrimStart();

        return expr.Length == 0 ? "" : $" DEFAULT {expr}";
    }

    /// <summary>
    /// Returns a self-assignment (<c>"col" = "col"</c>) for models without writable non-key columns,
    /// preferring a non-identity key column (identity columns cannot be assigned on SQL Server).
    /// </summary>
    public static string NoOpSet(EmitContext ctx)
    {
        for (var i = 0; i < ctx.Keys.Length; i++)
        {
            if (!ctx.Keys[i].IsIdentity)
                return $"{ctx.Keys[i].ColumnNameQuoted} = {ctx.Keys[i].ColumnNameQuoted}";
        }

        var fallback = ctx.Keys.Length > 0 ? ctx.Keys[0] : ctx.Columns[0];
        return $"{fallback.ColumnNameQuoted} = {fallback.ColumnNameQuoted}";
    }
}

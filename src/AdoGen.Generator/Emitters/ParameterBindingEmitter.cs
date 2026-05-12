using System.Text;
using AdoGen.Generator.Models;

namespace AdoGen.Generator.Emitters;

/// <summary>
/// Shared helper that produces parameter-binding C# source lines for both SqlServer and PostgreSql emitters.
/// Replaces the duplicated local closure helpers (ParamAdd, ParamAddForUpdate, ParamAddForDelete,
/// ParamAddBatchFlat) that previously lived inside each Domain emitter's Handle method.
/// </summary>
internal static class ParameterBindingEmitter
{
    /// <summary>
    /// Produces cmd.Parameters.Add calls for all columns (Insert / Upsert for PG, Insert for SS).
    /// The returned string ends with a newline.
    /// </summary>
    public static string BindAll(EmitContext ctx, string modelVar, int indent)
    {
        var prefix = new string(' ', indent);
        var sb = new StringBuilder();
        
        foreach (var col in ctx.Writables)
            sb.AppendLine($"{prefix}cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}({modelVar}.{col.Name}));");
        
        return sb.ToString();
    }

    /// <summary>
    /// Produces cmd.Parameters.Add calls for update: non-key-non-identity columns first, then keys,
    /// then the concurrency token (if any).
    /// The returned string ends with a newline.
    /// </summary>
    public static string BindForUpdate(EmitContext ctx, string modelVar, int indent)
    {
        var prefix = new string(' ', indent);
        var sb = new StringBuilder();
        
        foreach (var col in ctx.WritableNonKeyNonIdentities)
            sb.AppendLine($"{prefix}cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}({modelVar}.{col.Name}));");
        
        foreach (var col in ctx.Keys)
            sb.AppendLine($"{prefix}cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}({modelVar}.{col.Name}));");

        if (ctx.ConcurrencyToken is { } token)
            sb.AppendLine($"{prefix}cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{token.Name}({modelVar}.{token.Name}));");
        
        return sb.ToString();
    }

    /// <summary>
    /// Produces cmd.Parameters.Add calls for delete: key columns only, plus concurrency token if any.
    /// The returned string ends with a newline.
    /// </summary>
    public static string BindForDelete(EmitContext ctx, string modelVar, int indent)
    {
        var prefix = new string(' ', indent);
        var sb = new StringBuilder();
        
        foreach (var col in ctx.Keys)
            sb.AppendLine($"{prefix}cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}({modelVar}.{col.Name}));");

        if (ctx.ConcurrencyToken is { } token)
            sb.AppendLine($"{prefix}cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{token.Name}({modelVar}.{token.Name}));");
        
        return sb.ToString();
    }

    /// <summary>
    /// Produces cmd.Parameters.Add calls for key columns only (no concurrency token, no dynamic naming).
    /// Used for ExistsAsync where only the key columns are needed, with stable parameter names.
    /// The returned string ends with a newline.
    /// </summary>
    public static string BindKeys(EmitContext ctx, string modelVar, int indent)
    {
        var prefix = new string(' ', indent);
        var sb = new StringBuilder();
        
        foreach (var col in ctx.Keys)
            sb.AppendLine($"{prefix}cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}({modelVar}.{col.Name}));");
        
        return sb.ToString();
    }

    /// <summary>
    /// Produces cmd.Parameters.Add calls for SQL Server upsert:
    /// non-identity columns first, then any identity key columns.
    /// The returned string ends with a newline.
    /// </summary>
    public static string BindForUpsertSqlServer(EmitContext ctx, string modelVar, int indent)
    {
        var prefix = new string(' ', indent);
        var sb = new StringBuilder();
        
        foreach (var col in ctx.Writables)
            sb.AppendLine($"{prefix}cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}({modelVar}.{col.Name}));");
        
        foreach (var col in ctx.Keys)
        {
            if (col.IsIdentity)
                sb.AppendLine($"{prefix}cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}({modelVar}.{col.Name}));");
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Produces the inner body of a one-pass composite-key batch-delete loop.
    /// Each key column emits an sb.Append for the param name (with comma separator between keys)
    /// AND a cmd.Parameters.Add — both in the same loop iteration, eliminating the second pass.
    /// Must be placed inside a <c>for (var i = 0; i &lt; models.Count; i++)</c> loop after
    /// <c>sb.Append('(')</c> and before <c>sb.Append(')')</c>.
    /// </summary>
    public static string BindKeysInlineLoop(EmitContext ctx, string modelVar, string indexVar, string sbVar, int indent)
    {
        var prefix = new string(' ', indent);
        var sb = new StringBuilder();
        var last = ctx.Keys.Length - 1;

        for (var k = 0; k < ctx.Keys.Length; k++)
        {
            var col = ctx.Keys[k];
            var open = k == 0 ? "(" : ",";
            var close = k == last ? ")" : "";
            sb.AppendLine($"{prefix}{sbVar}.Append($\"{open}@p{{{indexVar}}}{close}\");");
            sb.AppendLine($"{prefix}cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}({modelVar}.{col.Name}, $\"@p{{{indexVar}}}\"));");
            sb.AppendLine($"{prefix}{indexVar}++;");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Produces the inner body of a one-pass batch-insert loop.
    /// Each writable column emits sb.Append for the comma separator + param name
    /// AND a cmd.Parameters.Add — both inline, eliminating the inner columnIndex loop.
    /// Must be placed inside a <c>for (var modelIndex = 0; ...)</c> loop after
    /// <c>sb.Append('(')</c> and before <c>sb.Append(')')</c>.
    /// </summary>
    public static string BindWritablesInlineLoop(EmitContext ctx, string modelVar, string indexVar, string sbVar, int indent)
    {
        var prefix = new string(' ', indent);
        var sb = new StringBuilder();
        var last = ctx.Writables.Length - 1;

        for (var k = 0; k < ctx.Writables.Length; k++)
        {
            var col = ctx.Writables[k];
            var open = k == 0 ? "(" : ",";
            var close = k == last ? ")" : "";
            sb.AppendLine($"{prefix}{sbVar}.Append($\"{open}@p{{{indexVar}}}{close}\");");
            sb.AppendLine($"{prefix}cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}({modelVar}.{col.Name}, $\"@p{{{indexVar}}}\"));");
            sb.AppendLine($"{prefix}{indexVar}++;");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Produces cmd.Parameters.Add calls for composite-key batch delete via unnest.
    /// Each key column gets a single array parameter (@{col}s) containing all values extracted from the list.
    /// e.g.: cmd.Parameters.Add(new NpgsqlParameter { ParameterName = "Ids", NpgsqlDbType = Array | Uuid, Value = models.Select(m => m.Id).ToArray() });
    /// </summary>
    public static string BindKeyArraysNpgsql(EmitContext ctx, string listVar, int indent)
    {
        var prefix = new string(' ', indent);
        var sb = new StringBuilder();

        foreach (var col in ctx.Keys)
        {
            var npgsqlDbType = col.SqlType; // not what we want — use DbType from profile
            // NpgsqlDbType enum member is stored per column in ctx.Profile.ParamsByProperty
            var dbTypeMember = ctx.Profile.ParamsByProperty[col.Name].DbType!.Value.EnumMember;
            sb.AppendLine($"{prefix}cmd.Parameters.Add(new global::Npgsql.NpgsqlParameter");
            sb.AppendLine($"{prefix}{{");
            sb.AppendLine($"{prefix}    ParameterName = \"{col.ParameterName}s\",");
            sb.AppendLine($"{prefix}    NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.{dbTypeMember},");
            sb.AppendLine($"{prefix}    Value = {listVar}.Select(static m => m.{col.Name}).ToArray(),");
            sb.AppendLine($"{prefix}}});");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Returns the estimated per-row character count for a batch insert value tuple,
    /// e.g. "(@p0,@p1,@p2)" — used to pre-size the StringBuilder.
    /// Formula: each param slot is ~5 chars (@p + up to 3 digits), plus 2 for the parens,
    /// minus 1 because the first column has no leading comma.
    /// </summary>
    public static int BatchInsertPerRowEstimate(EmitContext ctx)
        => ctx.Writables.Length * 5 + 2;
}

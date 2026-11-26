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
    public static string BindAll(EmitContext ctx, string modelVar, int indent, string cmdVar = "cmd")
    {
        var prefix = new string(' ', indent);
        var sb = new StringBuilder();
        
        foreach (var col in ctx.Writables)
            sb.AppendLine($"{prefix}{cmdVar}.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}({modelVar}.{col.Name}));");
        
        return sb.ToString();
    }

    /// <summary>
    /// Produces cmd.Parameters.Add calls for update: non-key-non-identity columns first, then keys,
    /// then the concurrency token (if any).
    /// The returned string ends with a newline.
    /// </summary>
    public static string BindForUpdate(EmitContext ctx, string modelVar, int indent, string cmdVar = "cmd")
    {
        var prefix = new string(' ', indent);
        var sb = new StringBuilder();
        
        foreach (var col in ctx.WritableNonKeyNonIdentities)
            sb.AppendLine($"{prefix}{cmdVar}.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}({modelVar}.{col.Name}));");
        
        foreach (var col in ctx.Keys)
            sb.AppendLine($"{prefix}{cmdVar}.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}({modelVar}.{col.Name}));");

        if (ctx.ConcurrencyToken is { } token)
            sb.AppendLine($"{prefix}{cmdVar}.Parameters.Add({ctx.FactoryClassName}.CreateParameter{token.Name}({modelVar}.{token.Name}));");
        
        return sb.ToString();
    }

    /// <summary>
    /// Produces cmd.Parameters.Add calls for delete: key columns only, plus concurrency token if any.
    /// The returned string ends with a newline.
    /// </summary>
    public static string BindForDelete(EmitContext ctx, string modelVar, int indent, string cmdVar = "cmd")
    {
        var prefix = new string(' ', indent);
        var sb = new StringBuilder();
        
        foreach (var col in ctx.Keys)
            sb.AppendLine($"{prefix}{cmdVar}.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}({modelVar}.{col.Name}));");

        if (ctx.ConcurrencyToken is { } token)
            sb.AppendLine($"{prefix}{cmdVar}.Parameters.Add({ctx.FactoryClassName}.CreateParameter{token.Name}({modelVar}.{token.Name}));");
        
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
    /// Produces cmd.Parameters.Add calls for upsert:
    /// non-identity columns first, then any identity key columns.
    /// The returned string ends with a newline.
    /// </summary>
    public static string BindForUpsert(EmitContext ctx, string modelVar, int indent, string cmdVar = "cmd")
    {
        var prefix = new string(' ', indent);
        var sb = new StringBuilder();
        
        foreach (var col in ctx.Writables)
            sb.AppendLine($"{prefix}{cmdVar}.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}({modelVar}.{col.Name}));");
        
        foreach (var col in ctx.Keys)
        {
            if (col.IsIdentity)
                sb.AppendLine($"{prefix}{cmdVar}.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}({modelVar}.{col.Name}));");
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
    public static string BindKeysInlineLoop(EmitContext ctx, string modelVar, string indexVar, string sbVar, string namesClass, int indent)
        => BindInlineLoop(ctx, ctx.Keys, modelVar, indexVar, sbVar, namesClass, indent);

    /// <summary>
    /// Emits, per column, a cached positional parameter name (<c>{namesClass}.Get(index++)</c>), its placement in the
    /// value tuple and the matching <c>cmd.Parameters.Add</c>. Names come from the runtime cache, so no strings are allocated.
    /// </summary>
    private static string BindInlineLoop(EmitContext ctx, System.Collections.Immutable.ImmutableArray<ColumnInfo> columns,
        string modelVar, string indexVar, string sbVar, string namesClass, int indent)
    {
        var prefix = new string(' ', indent);
        var sb = new StringBuilder();
        var last = columns.Length - 1;

        for (var k = 0; k < columns.Length; k++)
        {
            var col = columns[k];
            var open = k == 0 ? "'('" : "','";
            var close = k == last ? ".Append(')')" : "";
            sb.AppendLine($"{prefix}var name{k} = {namesClass}.Get({indexVar}++);");
            sb.AppendLine($"{prefix}{sbVar}.Append({open}).Append(name{k}){close};");
            sb.AppendLine($"{prefix}cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}({modelVar}.{col.Name}, name{k}));");
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
    public static string BindWritablesInlineLoop(EmitContext ctx, string modelVar, string indexVar, string sbVar, string namesClass, int indent)
        => BindInlineLoop(ctx, ctx.Writables, modelVar, indexVar, sbVar, namesClass, indent);

    /// <summary>
    /// Produces cmd.Parameters.Add calls for composite-key batch delete via unnest.
    /// Each key column gets a single typed array parameter (@{col}s) filled by a plain loop (no LINQ).
    /// </summary>
    public static string BindKeyArraysNpgsql(EmitContext ctx, string listVar, int indent)
    {
        var prefix = new string(' ', indent);
        var sb = new StringBuilder();

        foreach (var col in ctx.Keys)
        {
            var dbTypeMember = ctx.Profile.ParamsByProperty[col.Name].DbType!.Value.EnumMember;
            var arrayVar = $"{LowerFirst(col.Name)}Values";
            sb.AppendLine($"{prefix}var {arrayVar} = new {col.PropertyType}[{listVar}.Count];");
            sb.AppendLine($"{prefix}for (var i = 0; i < {listVar}.Count; i++) {arrayVar}[i] = {listVar}[i].{col.Name};");
            sb.AppendLine($"{prefix}cmd.Parameters.Add(new global::Npgsql.NpgsqlParameter");
            sb.AppendLine($"{prefix}{{");
            sb.AppendLine($"{prefix}    ParameterName = \"{col.ParameterName}s\",");
            sb.AppendLine($"{prefix}    NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.{dbTypeMember},");
            sb.AppendLine($"{prefix}    Value = {arrayVar},");
            sb.AppendLine($"{prefix}}});");
        }

        return sb.ToString().TrimEnd();
    }

    private static string LowerFirst(string s) => s.Length == 0 ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);

    /// <summary>
    /// Returns the estimated per-row character count for a batch insert value tuple,
    /// e.g. "(@p0,@p1,@p2)" — used to pre-size the StringBuilder.
    /// Each param slot is ~6 chars ("@p" + up to 4 digits), plus 2 for the parens.
    /// </summary>
    public static int BatchInsertPerRowEstimate(EmitContext ctx)
        => ctx.Writables.Length * 6 + 2;
}

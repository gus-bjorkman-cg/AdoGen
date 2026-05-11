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
    /// Produces cmd.Parameters.Add calls for update: non-key-non-identity columns first, then keys.
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
        
        return sb.ToString();
    }

    /// <summary>
    /// Produces cmd.Parameters.Add calls for delete: key columns only.
    /// The returned string ends with a newline.
    /// </summary>
    public static string BindForDelete(EmitContext ctx, string modelVar, int indent)
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
    /// Produces batched cmd.Parameters.Add calls with an incrementing index variable.
    /// Each column emits: CreateParameterX(model.X, $"@p{index}") followed by index++[suffix].
    /// </summary>
    public static string BindBatchFlat(EmitContext ctx, string modelVar, string indexVar, int indent, bool trimEnd = false)
    {
        var prefix = new string(' ', indent);
        var sb = new StringBuilder();
        
        foreach (var col in ctx.Writables)
        {
            sb.AppendLine($"{prefix}cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}({modelVar}.{col.Name}, $\"@p{{{indexVar}}}\"));");
            sb.AppendLine($"{prefix}{indexVar}++;");
        }
        
        return trimEnd ? sb.ToString().TrimEnd() : sb.ToString();
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

        for (var k = 0; k < ctx.Keys.Length; k++)
        {
            var col = ctx.Keys[k];
            if (k > 0) sb.AppendLine($"{prefix}{sbVar}.Append(',');");
            sb.AppendLine($"{prefix}{sbVar}.Append($\"@p{{{indexVar}}}\");");
            sb.AppendLine($"{prefix}cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}({modelVar}.{col.Name}, $\"@p{{{indexVar}}}\"));");
            sb.AppendLine($"{prefix}{indexVar}++;");
        }

        return sb.ToString().TrimEnd();
    }
}

using AdoGen.Generator.Models;

namespace AdoGen.Generator.Emitters;

/// <summary>
/// Emits the optional optimistic-concurrency parts of <c>PatchAsync</c> and the shared <c>{Dto}Patch</c> class.
/// The check is opt-in: callers supply the expected token via <c>WithExpected{Token}(...)</c>; when absent the
/// patch is a plain keyed UPDATE and the token column is left untouched.
/// </summary>
internal static class PatchConcurrency
{
    public static bool IsIntOrLong(string propertyType)
        => propertyType.TrimEnd('?') is "int" or "long" or "global::System.Int32" or "global::System.Int64";

    /// <summary>Members added to the shared patch class (provider-neutral).</summary>
    public static string BuildMembers(EmitContext ctx, string patchClassName)
    {
        if (ctx.ConcurrencyToken is not { } token) return "";

        var type = token.PropertyType.TrimEnd('?');
        var name = token.Name;

        if (IsIntOrLong(type))
        {
            return
                $"    /// <summary>Expected current value of <c>{name}</c>; when set, the patch only applies if it matches and the token is incremented.</summary>\n" +
                $"    public {type}? Expected{name} {{ get; private set; }}\n" +
                $"    /// <summary>Enables the optimistic-concurrency check: throws <c>AdoGenConcurrencyException</c> if the stored <c>{name}</c> differs.</summary>\n" +
                $"    public {patchClassName} WithExpected{name}({type} expected) {{ Expected{name} = expected; return this; }}\n";
        }

        return
            $"    /// <summary>Expected current value of <c>{name}</c>; when set, the patch only applies if it matches.</summary>\n" +
            $"    public {type}? Expected{name} {{ get; private set; }}\n" +
            $"    /// <summary>New value written to <c>{name}</c> when the concurrency check passes.</summary>\n" +
            $"    public {type}? Next{name} {{ get; private set; }}\n" +
            $"    /// <summary>Enables the optimistic-concurrency check: throws <c>AdoGenConcurrencyException</c> if the stored <c>{name}</c> differs, otherwise writes <paramref name=\"next\"/>.</summary>\n" +
            $"    public {patchClassName} WithExpected{name}({type} expected, {type} next) {{ Expected{name} = expected; Next{name} = next; return this; }}\n";
    }

    /// <summary>SET-list addition (appended after the masked columns, so <c>separator</c> is already ", ").</summary>
    public static string BuildSet(EmitContext ctx, string parameterPrefix)
    {
        if (ctx.ConcurrencyToken is not { } token) return "";

        var column = EscapeForCSharpString(token.ColumnNameQuoted);
        var name = token.Name;

        if (IsIntOrLong(token.PropertyType))
            return $"        if (patch.Expected{name}.HasValue) sb.Append(separator + \"{column} = @{token.ParameterName} + 1\");";

        var nextParam = $"{token.ParameterName}Next";
        return
            $"        if (patch.Expected{name}.HasValue)\n" +
            "        {\n" +
            $"            sb.Append(separator + \"{column} = @{nextParam}\");\n" +
            $"            cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{name}(patch.Next{name}!.Value, \"{parameterPrefix}{nextParam}\"));\n" +
            "        }";
    }

    /// <summary>WHERE-clause addition, appended to the command text builder.</summary>
    public static string BuildWhere(EmitContext ctx)
    {
        if (ctx.ConcurrencyToken is not { } token) return "";

        var column = EscapeForCSharpString(token.ColumnNameQuoted);
        return
            $"        if (patch.Expected{token.Name}.HasValue)\n" +
            "        {\n" +
            $"            sb.Append(\" AND {column} = @{token.ParameterName}\");\n" +
            $"            cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{token.Name}(patch.Expected{token.Name}.Value));\n" +
            "        }";
    }

    /// <summary>Execution tail: throws only when the caller opted in to the check and no row matched.</summary>
    public static string BuildExecute(EmitContext ctx, string exceptionTypeName)
    {
        if (ctx.ConcurrencyToken is not { } token)
            return "        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);";

        return
            "        var affected = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);\n" +
            $"        if (affected == 0 && patch.Expected{token.Name}.HasValue) throw new {exceptionTypeName}(\"{ctx.Profile.Schema}.{ctx.Profile.Table}\");\n" +
            "\n" +
            "        return affected;";
    }

    private static string EscapeForCSharpString(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

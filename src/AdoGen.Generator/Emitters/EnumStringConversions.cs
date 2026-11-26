using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Emitters;

/// <summary>
/// Emits the switch-based enum &lt;-&gt; string helpers used by <c>.AsString(size)</c> columns.
/// </summary>
internal static class EnumStringConversions
{
    public static string Build(ParamConfig cfg)
    {
        var (underlying, _) = cfg.PropertyType.UnwrapNullable();
        var enumType = (INamedTypeSymbol)underlying;
        var enumName = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var members = DistinctMembers(enumType);

        var toDb = new StringBuilder();
        var fromDb = new StringBuilder();
        foreach (var member in members)
        {
            toDb.AppendLine($"        {enumName}.{member} => \"{member}\",");
            fromDb.AppendLine($"        \"{member}\" => {enumName}.{member},");
        }

        return
            $$"""
                  /// <summary>
                  /// Converts <paramref name="value"/> to the member name stored in the {{cfg.PropertyName}} column.
                  /// </summary>
                  public static string {{ValueConversions.EnumToDbMethod(cfg)}}({{enumName}} value) => value switch
                  {
              {{toDb.ToString().TrimEnd()}}
                      _ => throw new global::System.ArgumentOutOfRangeException(nameof(value), value, "Value is not a defined {{enumType.Name}} member.")
                  };

                  /// <summary>
                  /// Converts a stored {{cfg.PropertyName}} column value back to <see cref="{{enumName}}"/>.
                  /// </summary>
                  public static {{enumName}} {{ValueConversions.EnumFromDbMethod(cfg)}}(string value) => value switch
                  {
              {{fromDb.ToString().TrimEnd()}}
                      _ => throw new global::System.InvalidOperationException("'" + value + "' is not a defined {{enumType.Name}} member.")
                  };

              """;
    }

    /// <summary>Enum members in declaration order, keeping the first name for aliased values.</summary>
    private static List<string> DistinctMembers(INamedTypeSymbol enumType)
    {
        var seen = new HashSet<object>();
        var members = new List<string>();

        foreach (var field in enumType.GetMembers().OfType<IFieldSymbol>())
        {
            if (!field.HasConstantValue || field.ConstantValue is null) continue;
            if (seen.Add(field.ConstantValue)) members.Add(field.Name);
        }

        return members;
    }
}

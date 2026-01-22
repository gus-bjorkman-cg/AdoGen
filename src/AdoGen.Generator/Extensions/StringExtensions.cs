using System;
using System.Linq;

namespace AdoGen.Generator.Extensions;

internal static class StringExtensions
{
    public static string PluralizeSimple(this string name)
    {
        if (name.EndsWith("y", StringComparison.OrdinalIgnoreCase) && 
            name.Length > 1 && 
            !"aeiou".Contains(char.ToLowerInvariant(name[^2]))) 
            return name[..^1] + "ies";
        
        if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("z", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
            return name + "es";
        
        return name + "s";
    }
}

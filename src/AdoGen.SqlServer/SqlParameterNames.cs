using System.ComponentModel;
using System.Globalization;
using System.Numerics;

namespace AdoGen.SqlServer;

/// <summary>
/// Cache of positional parameter names (<c>@p0</c>, <c>@p1</c>, …) used by generated multi-row commands.
/// Names are created on first use and shared by all DTOs, so building a command allocates no name strings after warm-up.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SqlParameterNames
{
    // SQL Server allows at most 2100 parameters per command.
    private const int MaxCached = 2100;
    private static readonly string?[] Names = new string?[MaxCached];

    /// <summary>Returns the parameter name <c>@p{index}</c>.</summary>
    public static string Get(int index)
    {
        if ((uint)index >= MaxCached) return Create(index);

        // Benign race: concurrent callers may both create an equal string; either result is valid.
        return Names[index] ??= Create(index);
    }

    /// <summary>
    /// Rounds <paramref name="count"/> up to the next power of two, capped at <paramref name="max"/>.
    /// Generated multi-row DELETEs pad to this size so that SQL text, and the cached plan, is shared across list sizes.
    /// </summary>
    public static int BucketSize(int count, int max)
    {
        if (count >= max) return max;
        var bucket = (int)BitOperations.RoundUpToPowerOf2((uint)count);
        return bucket > max ? max : bucket;
    }

    private static string Create(int index) => "@p" + index.ToString(CultureInfo.InvariantCulture);
}

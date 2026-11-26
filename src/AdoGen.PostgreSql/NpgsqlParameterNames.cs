using System.ComponentModel;
using System.Globalization;

namespace AdoGen.PostgreSql;

/// <summary>
/// Cache of positional parameter names (<c>@p0</c>, <c>@p1</c>, …) used by generated multi-row commands.
/// Names are created on first use and shared by all DTOs, so building a command allocates no name strings after warm-up.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class NpgsqlParameterNames
{
    // Indexes above this are still valid (PostgreSQL allows 65 535 parameters) but are not cached.
    private const int MaxCached = 8192;
    private static readonly string?[] Names = new string?[MaxCached];

    /// <summary>Returns the parameter name <c>@p{index}</c>.</summary>
    public static string Get(int index)
    {
        if ((uint)index >= MaxCached) return Create(index);

        // Benign race: concurrent callers may both create an equal string; either result is valid.
        return Names[index] ??= Create(index);
    }

    private static string Create(int index) => "@p" + index.ToString(CultureInfo.InvariantCulture);
}

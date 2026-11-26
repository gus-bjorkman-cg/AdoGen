using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace AdoGen.PostgreSql;

/// <summary>
/// Opens a <see cref="NpgsqlConnection"/> for the duration of one operation if it is not already open, and closes it
/// again on disposal only when this scope opened it. A connection that was already open is left untouched.
/// </summary>
/// <remarks>
/// Used by AdoGen runtime and generated code (Dapper-style connection lifetime). Allocation-free when the connection
/// is already open. Use with <c>await using var scope = await NpgsqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);</c>.
/// </remarks>
public readonly struct NpgsqlConnectionScope
{
    private readonly NpgsqlConnection? _openedConnection;

    private NpgsqlConnectionScope(NpgsqlConnection openedConnection) => _openedConnection = openedConnection;

    /// <summary>
    /// True when this scope opened the connection and will close it on disposal.
    /// </summary>
    public bool OpenedConnection => _openedConnection is not null;

    /// <summary>
    /// Opens <paramref name="connection"/> if it is not open.
    /// </summary>
    /// <param name="connection">The connection to open.</param>
    /// <param name="ct">Cancellation token passed to <see cref="NpgsqlConnection.OpenAsync(CancellationToken)"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<NpgsqlConnectionScope> OpenAsync(NpgsqlConnection connection, CancellationToken ct)
        => connection.State == ConnectionState.Open
            ? new ValueTask<NpgsqlConnectionScope>(default(NpgsqlConnectionScope))
            : OpenSlowAsync(connection, ct);

    private static async ValueTask<NpgsqlConnectionScope> OpenSlowAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        await connection.OpenAsync(ct).ConfigureAwait(false);
        return new NpgsqlConnectionScope(connection);
    }

    /// <summary>
    /// Closes the connection when this scope opened it; otherwise a no-op.
    /// </summary>
    public ConfiguredValueTaskAwaitable DisposeAsync()
        => (_openedConnection is null ? default : new ValueTask(_openedConnection.CloseAsync())).ConfigureAwait(false);
}

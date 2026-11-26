using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AdoGen.SqlServer;

/// <summary>
/// Opens a <see cref="SqlConnection"/> for the duration of one operation if it is not already open, and closes it
/// again on disposal only when this scope opened it. A connection that was already open is left untouched.
/// </summary>
/// <remarks>
/// Used by AdoGen runtime and generated code (Dapper-style connection lifetime). Allocation-free when the connection
/// is already open. Use with <c>await using var scope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);</c>.
/// </remarks>
public readonly struct SqlConnectionScope
{
    private readonly SqlConnection? _openedConnection;

    private SqlConnectionScope(SqlConnection openedConnection) => _openedConnection = openedConnection;

    /// <summary>
    /// True when this scope opened the connection and will close it on disposal.
    /// </summary>
    public bool OpenedConnection => _openedConnection is not null;

    /// <summary>
    /// Opens <paramref name="connection"/> if it is not open.
    /// </summary>
    /// <param name="connection">The connection to open.</param>
    /// <param name="ct">Cancellation token passed to <see cref="SqlConnection.OpenAsync(CancellationToken)"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<SqlConnectionScope> OpenAsync(SqlConnection connection, CancellationToken ct)
        => connection.State == ConnectionState.Open
            ? new ValueTask<SqlConnectionScope>(default(SqlConnectionScope))
            : OpenSlowAsync(connection, ct);

    private static async ValueTask<SqlConnectionScope> OpenSlowAsync(SqlConnection connection, CancellationToken ct)
    {
        await connection.OpenAsync(ct).ConfigureAwait(false);
        return new SqlConnectionScope(connection);
    }

    /// <summary>
    /// Closes the connection when this scope opened it; otherwise a no-op.
    /// </summary>
    public ConfiguredValueTaskAwaitable DisposeAsync()
        => (_openedConnection is null ? default : new ValueTask(_openedConnection.CloseAsync())).ConfigureAwait(false);
}

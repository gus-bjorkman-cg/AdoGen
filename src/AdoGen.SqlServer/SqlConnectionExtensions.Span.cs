using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AdoGen.SqlServer;

// ReadOnlySpan<SqlParameter> overloads: a collection expression such as [p1, p2] binds here without allocating an
// array, and iteration does not box an enumerator. Async methods cannot take spans, so each overload builds the
// command synchronously and hands ownership of it to an async helper that disposes it.
public static partial class SqlConnectionExtensions
{
    private static async ValueTask<List<T>> QueryOwnedAsync<T>(SqlCommand command, CancellationToken ct)
        where T : ISqlMapper<T>
    {
        await using (command.ConfigureAwait(false))
            return await command.QueryAsync<T>(ct).ConfigureAwait(false);
    }

    private static async ValueTask<T?> QueryFirstOrDefaultOwnedAsync<T>(SqlCommand command, CancellationToken ct)
        where T : ISqlMapper<T>
    {
        await using (command.ConfigureAwait(false))
            return await command.QueryFirstOrDefaultAsync<T>(ct).ConfigureAwait(false);
    }

    private static async ValueTask<SqlDataReader> QueryMultiOwnedAsync(SqlCommand command, CancellationToken ct)
    {
        await using (command.ConfigureAwait(false))
            return await command.QueryMultiAsync(ct).ConfigureAwait(false);
    }

    private static async ValueTask<int> ExecuteOwnedAsync(SqlCommand command, CancellationToken ct)
    {
        await using (command.ConfigureAwait(false))
        {
            if (command.Connection.State != ConnectionState.Open) await command.Connection.OpenAsync(ct).ConfigureAwait(false);

            return await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }

    private static async ValueTask<T?> ExecuteScalarOwnedAsync<T>(SqlCommand command, CancellationToken ct)
    {
        await using (command.ConfigureAwait(false))
        {
            if (command.Connection.State != ConnectionState.Open) await command.Connection.OpenAsync(ct).ConfigureAwait(false);

            var value = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return value is null or DBNull ? default : (T)value;
        }
    }

    private static async ValueTask<List<T?>> QueryScalarOwnedAsync<T>(SqlCommand command, CancellationToken ct)
    {
        await using (command.ConfigureAwait(false))
            return await command.QueryScalarAsync<T>(ct).ConfigureAwait(false);
    }

    private static async ValueTask<T?> QueryScalarFirstOrDefaultOwnedAsync<T>(SqlCommand command, CancellationToken ct)
    {
        await using (command.ConfigureAwait(false))
            return await command.QueryScalarFirstOrDefaultAsync<T>(ct).ConfigureAwait(false);
    }
}

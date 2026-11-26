using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace AdoGen.PostgreSql;

// ReadOnlySpan<NpgsqlParameter> overloads: a collection expression such as [p1, p2] binds here without allocating an
// array, and iteration does not box an enumerator. Async methods cannot take spans, so each overload builds the
// command synchronously and hands ownership of it to an async helper that disposes it.
public static partial class NpgsqlConnectionExtensions
{
    private static async ValueTask<List<T>> QueryOwnedAsync<T>(NpgsqlCommand command, CancellationToken ct)
        where T : INpgsqlMapper<T>
    {
        await using (command.ConfigureAwait(false))
            return await command.QueryAsync<T>(ct).ConfigureAwait(false);
    }

    private static async ValueTask<T?> QueryFirstOrDefaultOwnedAsync<T>(NpgsqlCommand command, CancellationToken ct)
        where T : INpgsqlMapper<T>
    {
        await using (command.ConfigureAwait(false))
            return await command.QueryFirstOrDefaultAsync<T>(ct).ConfigureAwait(false);
    }

    private static async ValueTask<NpgsqlDataReader> QueryMultiOwnedAsync(NpgsqlCommand command, CancellationToken ct)
    {
        await using (command.ConfigureAwait(false))
            return await command.QueryMultiAsync(ct).ConfigureAwait(false);
    }

    private static async IAsyncEnumerable<T> QueryUnbufferedOwnedAsync<T>(NpgsqlCommand command, [EnumeratorCancellation] CancellationToken ct)
        where T : INpgsqlMapper<T>
    {
        await using (command.ConfigureAwait(false))
        {
            await foreach (var item in command.QueryUnbufferedAsync<T>(ct).ConfigureAwait(false)) yield return item;
        }
    }

    private static async ValueTask<int> ExecuteOwnedAsync(NpgsqlCommand command, CancellationToken ct)
    {
        await using (command.ConfigureAwait(false))
        {
            await using var connectionScope = await NpgsqlConnectionScope.OpenAsync(command.Connection!, ct).ConfigureAwait(false);

            return await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }

    private static async ValueTask<T?> ExecuteScalarOwnedAsync<T>(NpgsqlCommand command, CancellationToken ct)
    {
        await using (command.ConfigureAwait(false))
        {
            await using var connectionScope = await NpgsqlConnectionScope.OpenAsync(command.Connection!, ct).ConfigureAwait(false);

            var value = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return ScalarConversion.Convert<T>(value);
        }
    }

    private static async ValueTask<List<T?>> QueryScalarOwnedAsync<T>(NpgsqlCommand command, CancellationToken ct)
    {
        await using (command.ConfigureAwait(false))
            return await command.QueryScalarAsync<T>(ct).ConfigureAwait(false);
    }

    private static async ValueTask<T?> QueryScalarFirstOrDefaultOwnedAsync<T>(NpgsqlCommand command, CancellationToken ct)
    {
        await using (command.ConfigureAwait(false))
            return await command.QueryScalarFirstOrDefaultAsync<T>(ct).ConfigureAwait(false);
    }
}

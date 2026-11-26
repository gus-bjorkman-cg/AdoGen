using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

namespace AdoGen.PostgreSql;

/// <summary>
/// Extensions class for NpgsqlCommand.
/// </summary>
public static class NpgsqlCommandExtensions
{
    extension(NpgsqlCommand command)
    {
        /// <summary>
        /// Executes the SQL and maps the objects by using the source generated mapper.
        /// </summary>
        /// <param name="ct"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async ValueTask<List<T>> QueryAsync<T>(CancellationToken ct)
            where T : INpgsqlMapper<T>
        {
            await using var connectionScope = await NpgsqlConnectionScope.OpenAsync(command.Connection!, ct).ConfigureAwait(false);

            await using var reader = await command
                .ExecuteReaderAsync(CommandBehavior.SingleResult, ct)
                .ConfigureAwait(false);

            if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return [];

            var ordinals = T.GetOrdinals(reader);
            var items = new List<T>();
            do items.Add(T.Map(reader, ordinals)); while (await reader.ReadAsync(ct).ConfigureAwait(false));

            return items;
        }

        /// <summary>
        /// Executes the SQL and maps the object by using the source generated mapper.
        /// </summary>
        /// <param name="ct"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async ValueTask<T?> QueryFirstOrDefaultAsync<T>(CancellationToken ct)
            where T : INpgsqlMapper<T>
        {
            await using var connectionScope = await NpgsqlConnectionScope.OpenAsync(command.Connection!, ct).ConfigureAwait(false);

            await using var reader = await command
                .ExecuteReaderAsync(
                    CommandBehavior.SingleResult | CommandBehavior.SingleRow, ct)
                .ConfigureAwait(false);

            if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return default;

            return T.Map(reader);
        }

        /// <summary>
        /// Executes the SQL and returns a list of scalar values from the first column using Npgsql's native type mapping.
        /// Handles DBNull by returning default(T) for that row.
        /// </summary>
        /// <param name="ct"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async ValueTask<List<T?>> QueryScalarAsync<T>(CancellationToken ct)
        {
            await using var connectionScope = await NpgsqlConnectionScope.OpenAsync(command.Connection!, ct).ConfigureAwait(false);

            await using var reader = await command
                .ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult, ct)
                .ConfigureAwait(false);

            if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return [];

            var items = new List<T?>();
            
            do items.Add(reader.IsDBNull(0) ? default : reader.GetFieldValue<T>(0));
            while (await reader.ReadAsync(ct).ConfigureAwait(false));

            return items;
        }

        /// <summary>
        /// Executes the SQL and returns the first scalar value from the first column, or default if no rows.
        /// Handles DBNull by returning default(T).
        /// </summary>
        /// <param name="ct"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async ValueTask<T?> QueryScalarFirstOrDefaultAsync<T>(CancellationToken ct)
        {
            await using var connectionScope = await NpgsqlConnectionScope.OpenAsync(command.Connection!, ct).ConfigureAwait(false);

            await using var reader = await command
                .ExecuteReaderAsync(
                    CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, ct)
                .ConfigureAwait(false);

            if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return default;

            return reader.IsDBNull(0) ? default : reader.GetFieldValue<T>(0);
        }

        /// <summary>
        /// Opens the connection if not opened and executes the reader.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async ValueTask<NpgsqlDataReader> QueryMultiAsync(CancellationToken ct)
        {
            var connectionScope = await NpgsqlConnectionScope.OpenAsync(command.Connection!, ct).ConfigureAwait(false);

            try
            {
                // When the connection was opened here, the reader owns it and closes it on disposal.
                var behavior = connectionScope.OpenedConnection ? CommandBehavior.CloseConnection : CommandBehavior.Default;
                return await command.ExecuteReaderAsync(behavior, ct).ConfigureAwait(false);
            }
            catch
            {
                await connectionScope.DisposeAsync();
                throw;
            }
        }

        /// <summary>
        /// Executes the SQL and streams the rows, mapping each one with the source generated mapper as it is read.
        /// Nothing is buffered: use this for large result sets. The connection is opened on the first
        /// <c>MoveNextAsync</c> if needed, and the reader is disposed (and the connection closed, if it was opened here)
        /// when enumeration completes or the consumer stops early.
        /// </summary>
        /// <remarks>
        /// The connection cannot run other commands while the enumeration is in progress.
        /// When enumeration is stopped early, Npgsql consumes the remaining rows while disposing the reader; add a
        /// <c>LIMIT</c> or cancel the token if the remaining result is large.
        /// </remarks>
        /// <param name="ct"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>An async stream of mapped rows.</returns>
        public async IAsyncEnumerable<T> QueryUnbufferedAsync<T>([EnumeratorCancellation] CancellationToken ct)
            where T : INpgsqlMapper<T>
        {
            await using var connectionScope = await NpgsqlConnectionScope.OpenAsync(command.Connection!, ct).ConfigureAwait(false);

            await using var reader = await command
                .ExecuteReaderAsync(CommandBehavior.SingleResult, ct)
                .ConfigureAwait(false);

            if (!await reader.ReadAsync(ct).ConfigureAwait(false)) yield break;

            var ordinals = T.GetOrdinals(reader);
            do yield return T.Map(reader, ordinals); while (await reader.ReadAsync(ct).ConfigureAwait(false));
        }

        /// <summary>
        /// Creates an Npgsql parameter and adds it to the command.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="size"></param>
        public void CreateParameter(string name, object? value, NpgsqlDbType type, int size = 0)
        {
            var p = new NpgsqlParameter
            {
                ParameterName = name,
                NpgsqlDbType = type,
                Value = value ?? DBNull.Value,
            };

            if (size > 0) p.Size = size;

            command.Parameters.Add(p);
        }
    }
}

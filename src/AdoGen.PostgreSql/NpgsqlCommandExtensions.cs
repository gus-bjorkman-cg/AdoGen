using System;
using System.Collections.Generic;
using System.Data;
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
            if (command.Connection!.State != ConnectionState.Open)
                await command.Connection.OpenAsync(ct).ConfigureAwait(false);

            await using var reader = await command
                .ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult, ct)
                .ConfigureAwait(false);

            if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return [];

            var items = new List<T>();
            do items.Add(T.Map(reader)); while (await reader.ReadAsync(ct).ConfigureAwait(false));

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
            if (command.Connection!.State != ConnectionState.Open)
                await command.Connection.OpenAsync(ct).ConfigureAwait(false);

            await using var reader = await command
                .ExecuteReaderAsync(
                    CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, ct)
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
            if (command.Connection!.State != ConnectionState.Open)
                await command.Connection.OpenAsync(ct).ConfigureAwait(false);

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
            if (command.Connection!.State != ConnectionState.Open)
                await command.Connection.OpenAsync(ct).ConfigureAwait(false);

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
            if (command.Connection!.State != ConnectionState.Open)
                await command.Connection.OpenAsync(ct).ConfigureAwait(false);

            return await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
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

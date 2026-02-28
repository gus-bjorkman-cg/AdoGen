using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace AdoGen.PostgreSql;

/// <summary>
/// Extensions class for NpgsqlConnection.
/// </summary>
public static class NpgsqlConnectionExtensions
{
    extension(NpgsqlConnection connection)
    {
        /// <summary>
        /// Creates the database table.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask CreateTableAsync<T>(CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
            where T : INpgsqlDomainModel<T> =>
            await T.CreateTableAsync(connection, ct, transaction, commandTimeout).ConfigureAwait(false);

        /// <summary>
        /// Inserts a database record.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask InsertAsync<T>(T model, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
            where T : INpgsqlDomainModel<T> =>
            await T.InsertAsync(model, connection, ct, transaction, commandTimeout).ConfigureAwait(false);

        /// <summary>
        /// Inserts multiple database records in one roundtrip.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask InsertAsync<T>(List<T> models, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
            where T : INpgsqlDomainModel<T> =>
            await T.InsertAsync(models, connection, ct, transaction, commandTimeout).ConfigureAwait(false);

        /// <summary>
        /// Updates a database record.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask UpdateAsync<T>(T model, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
            where T : INpgsqlDomainModel<T> =>
            await T.UpdateAsync(model, connection, ct, transaction, commandTimeout).ConfigureAwait(false);

        /// <summary>
        /// Inserts or updates a database record.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask UpsertAsync<T>(T model, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
            where T : INpgsqlDomainModel<T> =>
            await T.UpsertAsync(model, connection, ct, transaction, commandTimeout).ConfigureAwait(false);

        /// <summary>
        /// Deletes a database record.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask DeleteAsync<T>(T model, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
            where T : INpgsqlDomainModel<T> =>
            await T.DeleteAsync(model, connection, ct, transaction, commandTimeout).ConfigureAwait(false);

        /// <summary>
        /// Truncates a database table.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask TruncateAsync<T>(CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
            where T : INpgsqlDomainModel<T> =>
            await T.TruncateAsync(connection, ct, transaction, commandTimeout).ConfigureAwait(false);

        /// <summary>
        /// Opens the connection if not opened, executes the SQL and maps the objects by using the source generated mapper.
        /// </summary>
        public async ValueTask<List<T>> QueryAsync<T>(
            string sql,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            NpgsqlTransaction? transaction = null,
            int? commandTimeout = null)
            where T : INpgsqlResult<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            return await command.QueryAsync<T>(ct);
        }

        /// <summary>
        /// Opens the connection if not opened, executes the SQL and maps the objects by using the source generated mapper.
        /// </summary>
        public async ValueTask<List<T>> QueryAsync<T>(
            string sql,
            NpgsqlParameter parameter,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            NpgsqlTransaction? transaction = null,
            int? commandTimeout = null)
            where T : INpgsqlResult<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            command.Parameters.Add(parameter);
            return await command.QueryAsync<T>(ct);
        }

        /// <summary>
        /// Opens the connection if not opened, executes the SQL and maps the objects by using the source generated mapper.
        /// </summary>
        public async ValueTask<List<T>> QueryAsync<T>(
            string sql,
            IEnumerable<NpgsqlParameter> parameters,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            NpgsqlTransaction? transaction = null,
            int? commandTimeout = null)
            where T : INpgsqlResult<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            foreach (var parameter in parameters) command.Parameters.Add(parameter);
            return await command.QueryAsync<T>(ct);
        }

        /// <summary>
        /// Opens the connection if not opened, executes the SQL and maps the object by using the source generated mapper.
        /// </summary>
        public async ValueTask<T?> QueryFirstOrDefaultAsync<T>(
            string sql,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            NpgsqlTransaction? transaction = null,
            int? commandTimeout = null)
            where T : INpgsqlResult<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            return await command.QueryFirstOrDefaultAsync<T>(ct);
        }

        /// <summary>
        /// Opens the connection if not opened, executes the SQL and maps the object by using the source generated mapper.
        /// </summary>
        public async ValueTask<T?> QueryFirstOrDefaultAsync<T>(
            string sql,
            NpgsqlParameter parameter,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            NpgsqlTransaction? transaction = null,
            int? commandTimeout = null)
            where T : INpgsqlResult<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            command.Parameters.Add(parameter);
            return await command.QueryFirstOrDefaultAsync<T>(ct);
        }

        /// <summary>
        /// Opens the connection if not opened, executes the SQL and maps the object by using the source generated mapper.
        /// </summary>
        public async ValueTask<T?> QueryFirstOrDefaultAsync<T>(
            string sql,
            IEnumerable<NpgsqlParameter> parameters,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            NpgsqlTransaction? transaction = null,
            int? commandTimeout = null)
            where T : INpgsqlResult<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            foreach (var parameter in parameters) command.Parameters.Add(parameter);
            return await command.QueryFirstOrDefaultAsync<T>(ct);
        }

        /// <summary>
        /// Opens the connection if not opened and executes the reader.
        /// </summary>
        public async ValueTask<NpgsqlDataReader> QueryMultiAsync(
            string sql,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            NpgsqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            return await command.QueryMultiAsync(ct);
        }

        /// <summary>
        /// Opens the connection if not opened and executes the reader.
        /// </summary>
        public async ValueTask<NpgsqlDataReader> QueryMultiAsync(
            string sql,
            NpgsqlParameter parameter,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            NpgsqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            command.Parameters.Add(parameter);
            return await command.QueryMultiAsync(ct);
        }

        /// <summary>
        /// Opens the connection if not opened and executes the reader.
        /// </summary>
        public async ValueTask<NpgsqlDataReader> QueryMultiAsync(
            string sql,
            IEnumerable<NpgsqlParameter> parameters,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            NpgsqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            foreach (var parameter in parameters) command.Parameters.Add(parameter);
            return await command.QueryMultiAsync(ct);
        }

        /// <summary>
        /// Creates an NpgsqlCommand.
        /// </summary>
        public NpgsqlCommand CreateCommand(
            string sql,
            CommandType commandType = CommandType.Text,
            NpgsqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = commandType;

            if (transaction != null) command.Transaction = transaction;
            if (commandTimeout != null) command.CommandTimeout = commandTimeout.Value;

            return command;
        }
    }
}


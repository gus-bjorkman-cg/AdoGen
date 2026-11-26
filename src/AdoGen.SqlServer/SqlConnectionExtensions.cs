using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AdoGen.SqlServer;

/// <summary>
/// Extensions class for SqlConnection. 
/// </summary>
public static partial class SqlConnectionExtensions
{
    extension(SqlConnection connection)
    {
        /// <summary>
        /// Creates the database table.
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask CreateTableAsync<T>(CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
            where T : ISqlDomainModel<T>
            => T.CreateTableAsync(connection, ct, transaction, commandTimeout);

        /// <summary>
        /// Inserts a database record.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="ct"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<int> InsertAsync<T>(T model, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
            where T : ISqlDomainModel<T>
            => T.InsertAsync(model, connection, ct, transaction, commandTimeout);

        /// <summary>
        /// Inserts multiple database records in one roundtrip.
        /// </summary>
        /// <param name="models"></param>
        /// <param name="ct"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<int> InsertAsync<T>(List<T> models, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
            where T : ISqlDomainModel<T>
            => T.InsertAsync(models, connection, ct, transaction, commandTimeout);
        
        /// <summary>
        /// Updates a database record and returns <see cref="SqlWriteResult"/> instead of throwing on a missing row
        /// or concurrency conflict.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<SqlWriteResult> TryUpdateAsync<T>(T model, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
            where T : ISqlDomainModel<T>
            => T.TryUpdateAsync(model, connection, ct, transaction, commandTimeout);

        /// <summary>
        /// Deletes a database record and returns <see cref="SqlWriteResult"/> instead of throwing on a missing row
        /// or concurrency conflict.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<SqlWriteResult> TryDeleteAsync<T>(T model, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
            where T : ISqlDomainModel<T>
            => T.TryDeleteAsync(model, connection, ct, transaction, commandTimeout);

        /// <summary>
        /// Updates a database record.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="ct"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="SqlConcurrencyException">
        /// Thrown when the DTO has a concurrency token configured and 0 rows were affected,
        /// indicating the row was modified or deleted by another process.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<int> UpdateAsync<T>(T model, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
            where T : ISqlDomainModel<T>
            => T.UpdateAsync(model, connection, ct, transaction, commandTimeout);

        /// <summary>
        /// Inserts or updates a database record.
        /// </summary>
        /// <remarks>
        /// When the DTO has a concurrency token configured, <c>UpsertAsync</c> does <b>not</b>
        /// enforce the concurrency check — it performs an unconditional UPDATE + INSERT fallback.
        /// Use <c>UpdateAsync</c> instead if you require optimistic concurrency protection.
        /// </remarks>
        /// <param name="model"></param>
        /// <param name="ct"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<int> UpsertAsync<T>(T model, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
            where T : ISqlDomainModel<T>
            => T.UpsertAsync(model, connection, ct, transaction, commandTimeout);

        /// <summary>
        /// Deletes a database record.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="ct"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="SqlConcurrencyException">
        /// Thrown when the DTO has a concurrency token configured and 0 rows were affected,
        /// indicating the row was modified or deleted by another process.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<int> DeleteAsync<T>(T model, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
            where T : ISqlDomainModel<T>
            => T.DeleteAsync(model, connection, ct, transaction, commandTimeout);

        /// <summary>
        /// Truncates a database table.
        /// </summary>
        /// <param name="ct"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<int> TruncateAsync<T>(CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
            where T : ISqlDomainModel<T>
            => T.TruncateAsync(connection, ct, transaction, commandTimeout);

        /// <summary>
        /// Inserts a database record and returns the inserted row with server-generated values populated.
        /// SQL Server uses OUTPUT INSERTED.* to retrieve the row after insertion.
        /// </summary>
        /// <remarks>
        /// Single-row only. For bulk inserts see bulk copy operations.
        /// Note: SQL Server OUTPUT INSERTED.* fails if the target table has certain triggers.
        /// </remarks>
        /// <exception cref="InvalidOperationException"> Thrown when affected rows are 0. </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<T> InsertAndReturnAsync<T>(T model, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
            where T : ISqlDomainModel<T>
            => T.InsertAndReturnAsync(model, connection, ct, transaction, commandTimeout);

        /// <summary>
        /// Opens the connection if not opened, executes the SQL and maps the objects by using the source generated mapper.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="ct"></param>
        /// <param name="commandType"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>An empty list or a list with mapped object from the database</returns>
        public async ValueTask<List<T>> QueryAsync<T>(
            string sql, 
            CancellationToken ct, 
            CommandType commandType = CommandType.Text, 
            SqlTransaction? transaction = null,
            int? commandTimeout = null) 
            where T : ISqlMapper<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            
            return await command.QueryAsync<T>(ct).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Opens the connection if not opened, executes the SQL and maps the objects by using the source generated mapper.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameter"></param>
        /// <param name="ct"></param>
        /// <param name="commandType"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>An empty list or a list with mapped object from the database</returns>
        public async ValueTask<List<T>> QueryAsync<T>(
            string sql, 
            SqlParameter parameter, 
            CancellationToken ct, 
            CommandType commandType = CommandType.Text, 
            SqlTransaction? transaction = null,
            int? commandTimeout = null) 
            where T : ISqlMapper<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            command.Parameters.Add(parameter);
            
            return await command.QueryAsync<T>(ct).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Opens the connection if not opened, executes the SQL and maps the objects by using the source generated mapper.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <param name="ct"></param>
        /// <param name="commandType"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>An empty list or a list with mapped object from the database</returns>
        public async ValueTask<List<T>> QueryAsync<T>(
            string sql, 
            IEnumerable<SqlParameter> parameters, 
            CancellationToken ct, 
            CommandType commandType = CommandType.Text, 
            SqlTransaction? transaction = null,
            int? commandTimeout = null) 
            where T : ISqlMapper<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            
            foreach (var parameter in parameters) command.Parameters.Add(parameter);
            
            return await command.QueryAsync<T>(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Opens the connection if not opened, executes the SQL and maps the object by using the source generated mapper.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="ct"></param>
        /// <param name="commandType"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Null or the mapped object from the database</returns>
        public async ValueTask<T?> QueryFirstOrDefaultAsync<T>(
            string sql, 
            CancellationToken ct, 
            CommandType commandType = CommandType.Text, 
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
            where T : ISqlMapper<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            
            return await command.QueryFirstOrDefaultAsync<T>(ct).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Opens the connection if not opened, executes the SQL and maps the object by using the source generated mapper.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameter"></param>
        /// <param name="ct"></param>
        /// <param name="commandType"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Null or the mapped object from the database</returns>
        public async ValueTask<T?> QueryFirstOrDefaultAsync<T>(
            string sql, 
            SqlParameter parameter, 
            CancellationToken ct, 
            CommandType commandType = CommandType.Text, 
            SqlTransaction? transaction = null,
            int? commandTimeout = null) 
            where T : ISqlMapper<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            command.Parameters.Add(parameter);
            
            return await command.QueryFirstOrDefaultAsync<T>(ct).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Opens the connection if not opened, executes the SQL and maps the object by using the source generated mapper.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <param name="ct"></param>
        /// <param name="commandType"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Null or the mapped object from the database</returns>
        public async ValueTask<T?> QueryFirstOrDefaultAsync<T>(
            string sql,
            IEnumerable<SqlParameter> parameters, 
            CancellationToken ct, 
            CommandType commandType = CommandType.Text, 
            SqlTransaction? transaction = null,
            int? commandTimeout = null) 
            where T : ISqlMapper<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            
            foreach (var parameter in parameters) command.Parameters.Add(parameter);
            
            return await command.QueryFirstOrDefaultAsync<T>(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Opens the connection if not opened and executes the reader.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="ct"></param>
        /// <param name="commandType"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public async ValueTask<SqlDataReader> QueryMultiAsync(
            string sql, 
            CancellationToken ct, 
            CommandType commandType = CommandType.Text, 
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            
            return await command.QueryMultiAsync(ct).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Opens the connection if not opened and executes the reader.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameter"></param>
        /// <param name="ct"></param>
        /// <param name="commandType"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public async ValueTask<SqlDataReader> QueryMultiAsync(
            string sql, 
            SqlParameter parameter, 
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            command.Parameters.Add(parameter);
            
            return await command.QueryMultiAsync(ct).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Opens the connection if not opened and executes the reader.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <param name="ct"></param>
        /// <param name="commandType"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public async ValueTask<SqlDataReader> QueryMultiAsync(
            string sql, 
            IEnumerable<SqlParameter> parameters, 
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            
            foreach (var parameter in parameters) command.Parameters.Add(parameter);
            
            return await command.QueryMultiAsync(ct).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Executes the SQL and streams the mapped rows without buffering them in a list.
        /// See <see cref="SqlCommandExtensions"/> <c>QueryUnbufferedAsync</c> for connection and early-exit behaviour.
        /// </summary>
        /// <returns>An async stream of mapped rows.</returns>
        public async IAsyncEnumerable<T> QueryUnbufferedAsync<T>(
            string sql,
            [EnumeratorCancellation] CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
            where T : ISqlMapper<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);

            await foreach (var item in command.QueryUnbufferedAsync<T>(ct).ConfigureAwait(false)) yield return item;
        }

        /// <summary>
        /// Executes the SQL and streams the mapped rows without buffering them in a list.
        /// See <see cref="SqlCommandExtensions"/> <c>QueryUnbufferedAsync</c> for connection and early-exit behaviour.
        /// </summary>
        /// <returns>An async stream of mapped rows.</returns>
        public async IAsyncEnumerable<T> QueryUnbufferedAsync<T>(
            string sql,
            SqlParameter parameter,
            [EnumeratorCancellation] CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
            where T : ISqlMapper<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            command.Parameters.Add(parameter);

            await foreach (var item in command.QueryUnbufferedAsync<T>(ct).ConfigureAwait(false)) yield return item;
        }

        /// <summary>
        /// Executes the SQL and streams the mapped rows without buffering them in a list.
        /// See <see cref="SqlCommandExtensions"/> <c>QueryUnbufferedAsync</c> for connection and early-exit behaviour.
        /// </summary>
        /// <returns>An async stream of mapped rows.</returns>
        public async IAsyncEnumerable<T> QueryUnbufferedAsync<T>(
            string sql,
            IEnumerable<SqlParameter> parameters,
            [EnumeratorCancellation] CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
            where T : ISqlMapper<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);

            foreach (var parameter in parameters) command.Parameters.Add(parameter);

            await foreach (var item in command.QueryUnbufferedAsync<T>(ct).ConfigureAwait(false)) yield return item;
        }

        /// <summary>
        /// Executes the SQL and streams the mapped rows without buffering them in a list.
        /// See <see cref="SqlCommandExtensions"/> <c>QueryUnbufferedAsync</c> for connection and early-exit behaviour.
        /// </summary>
        /// <returns>An async stream of mapped rows.</returns>
        public IAsyncEnumerable<T> QueryUnbufferedAsync<T>(
            string sql,
            ReadOnlySpan<SqlParameter> parameters,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
            where T : ISqlMapper<T>
            => QueryUnbufferedOwnedAsync<T>(connection.CreateCommandWithParameters(sql, parameters, commandType, transaction, commandTimeout), ct);

        /// <summary>
        /// Creates a sql command with supplied parameters.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="commandType"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public SqlCommand CreateCommand(
            string sql,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = commandType;
            
            if (transaction != null) command.Transaction = transaction;
            if (commandTimeout != null) command.CommandTimeout = commandTimeout.Value;
            
            return command;
        }
        
        /// <summary>
        /// Opens the connection if not opened and executes the SQL and returns the number of affected rows.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="ct"></param>
        /// <param name="commandType"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public async ValueTask<int> ExecuteAsync(
            string sql,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
            
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            
            return await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Opens the connection if not opened and executes the SQL and returns the number of affected rows.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameter"></param>
        /// <param name="ct"></param>
        /// <param name="commandType"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public async ValueTask<int> ExecuteAsync(
            string sql,
            SqlParameter parameter,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
            
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            command.Parameters.Add(parameter);
            
            return await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Opens the connection if not opened and executes the SQL and returns the number of affected rows.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <param name="ct"></param>
        /// <param name="commandType"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public async ValueTask<int> ExecuteAsync(
            string sql,
            IEnumerable<SqlParameter> parameters,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
            
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            
            foreach (var parameter in parameters) command.Parameters.Add(parameter);
            
            return await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Opens the connection if not opened, executes the SQL and returns the first column of the first row,
        /// or <c>default</c> when there are no rows or the value is NULL. A numeric value of a different CLR type (e.g. <c>long</c> from <c>COUNT_BIG</c>) is converted to a numeric <typeparamref name="T"/> with an overflow check; other mismatches throw <see cref="InvalidCastException"/>.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="ct"></param>
        /// <param name="commandType"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public async ValueTask<T?> ExecuteScalarAsync<T>(
            string sql,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
            
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            
            var value = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return ScalarConversion.Convert<T>(value);
        }

        /// <summary>
        /// Opens the connection if not opened, executes the SQL and returns the first column of the first row,
        /// or <c>default</c> when there are no rows or the value is NULL. A numeric value of a different CLR type (e.g. <c>long</c> from <c>COUNT_BIG</c>) is converted to a numeric <typeparamref name="T"/> with an overflow check; other mismatches throw <see cref="InvalidCastException"/>.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameter"></param>
        /// <param name="ct"></param>
        /// <param name="commandType"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public async ValueTask<T?> ExecuteScalarAsync<T>(
            string sql,
            SqlParameter parameter,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
            
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            command.Parameters.Add(parameter);
            
            var value = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return ScalarConversion.Convert<T>(value);
        }
        
        /// <summary>
        /// Opens the connection if not opened, executes the SQL and returns the first column of the first row,
        /// or <c>default</c> when there are no rows or the value is NULL. A numeric value of a different CLR type (e.g. <c>long</c> from <c>COUNT_BIG</c>) is converted to a numeric <typeparamref name="T"/> with an overflow check; other mismatches throw <see cref="InvalidCastException"/>.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <param name="ct"></param>
        /// <param name="commandType"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
        public async ValueTask<T?> ExecuteScalarAsync<T>(
            string sql,
            IEnumerable<SqlParameter> parameters,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
            
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
                        
            foreach (var parameter in parameters) command.Parameters.Add(parameter);
            
            var value = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return ScalarConversion.Convert<T>(value);
        }

        /// <summary>
        /// Executes the SQL and returns a list of scalar values from the first column.
        /// Use for primitive/scalar types (int, string, Guid, etc.). Handles DBNull as default(T).
        /// </summary>
        public async ValueTask<List<T?>> QueryScalarAsync<T>(
            string sql,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            return await command.QueryScalarAsync<T>(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the SQL and returns a list of scalar values from the first column.
        /// Use for primitive/scalar types (int, string, Guid, etc.). Handles DBNull as default(T).
        /// </summary>
        public async ValueTask<List<T?>> QueryScalarAsync<T>(
            string sql,
            SqlParameter parameter,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            command.Parameters.Add(parameter);
            return await command.QueryScalarAsync<T>(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the SQL and returns a list of scalar values from the first column.
        /// Use for primitive/scalar types (int, string, Guid, etc.). Handles DBNull as default(T).
        /// </summary>
        public async ValueTask<List<T?>> QueryScalarAsync<T>(
            string sql,
            IEnumerable<SqlParameter> parameters,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            foreach (var parameter in parameters) command.Parameters.Add(parameter);
            return await command.QueryScalarAsync<T>(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the SQL and returns the first scalar value from the first column, or default if no rows.
        /// Use for primitive/scalar types (int, string, Guid, etc.). Handles DBNull as default(T).
        /// </summary>
        public async ValueTask<T?> QueryScalarFirstOrDefaultAsync<T>(
            string sql,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            return await command.QueryScalarFirstOrDefaultAsync<T>(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the SQL and returns the first scalar value from the first column, or default if no rows.
        /// Use for primitive/scalar types (int, string, Guid, etc.). Handles DBNull as default(T).
        /// </summary>
        public async ValueTask<T?> QueryScalarFirstOrDefaultAsync<T>(
            string sql,
            SqlParameter parameter,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            command.Parameters.Add(parameter);
            return await command.QueryScalarFirstOrDefaultAsync<T>(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the SQL and returns the first scalar value from the first column, or default if no rows.
        /// Use for primitive/scalar types (int, string, Guid, etc.). Handles DBNull as default(T).
        /// </summary>
        public async ValueTask<T?> QueryScalarFirstOrDefaultAsync<T>(
            string sql,
            IEnumerable<SqlParameter> parameters,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            foreach (var parameter in parameters) command.Parameters.Add(parameter);
            return await command.QueryScalarFirstOrDefaultAsync<T>(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Opens the connection if not opened, executes the SQL and maps the objects by using the source generated mapper.
        /// </summary>
        /// <returns>An empty list or a list with mapped object from the database</returns>
        public ValueTask<List<T>> QueryAsync<T>(
            string sql,
            ReadOnlySpan<SqlParameter> parameters,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
            where T : ISqlMapper<T>
            => QueryOwnedAsync<T>(connection.CreateCommandWithParameters(sql, parameters, commandType, transaction, commandTimeout), ct);

        /// <summary>
        /// Opens the connection if not opened, executes the SQL and maps the object by using the source generated mapper.
        /// </summary>
        /// <returns>Null or the mapped object from the database</returns>
        public ValueTask<T?> QueryFirstOrDefaultAsync<T>(
            string sql,
            ReadOnlySpan<SqlParameter> parameters,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
            where T : ISqlMapper<T>
            => QueryFirstOrDefaultOwnedAsync<T>(connection.CreateCommandWithParameters(sql, parameters, commandType, transaction, commandTimeout), ct);

        /// <summary>
        /// Opens the connection if not opened and executes the reader.
        /// </summary>
        public ValueTask<SqlDataReader> QueryMultiAsync(
            string sql,
            ReadOnlySpan<SqlParameter> parameters,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
            => QueryMultiOwnedAsync(connection.CreateCommandWithParameters(sql, parameters, commandType, transaction, commandTimeout), ct);

        /// <summary>
        /// Opens the connection if not opened and executes the SQL and returns the number of affected rows.
        /// </summary>
        public ValueTask<int> ExecuteAsync(
            string sql,
            ReadOnlySpan<SqlParameter> parameters,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
            => ExecuteOwnedAsync(connection.CreateCommandWithParameters(sql, parameters, commandType, transaction, commandTimeout), ct);

        /// <summary>
        /// Opens the connection if not opened, executes the SQL and returns the first column of the first row,
        /// or <c>default</c> when there are no rows or the value is NULL. A numeric value of a different CLR type (e.g. <c>long</c> from <c>COUNT_BIG</c>) is converted to a numeric <typeparamref name="T"/> with an overflow check; other mismatches throw <see cref="InvalidCastException"/>.
        /// </summary>
        public ValueTask<T?> ExecuteScalarAsync<T>(
            string sql,
            ReadOnlySpan<SqlParameter> parameters,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
            => ExecuteScalarOwnedAsync<T>(connection.CreateCommandWithParameters(sql, parameters, commandType, transaction, commandTimeout), ct);

        /// <summary>
        /// Executes the SQL and returns a list of scalar values from the first column.
        /// Use for primitive/scalar types (int, string, Guid, etc.). Handles DBNull as default(T).
        /// </summary>
        public ValueTask<List<T?>> QueryScalarAsync<T>(
            string sql,
            ReadOnlySpan<SqlParameter> parameters,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
            => QueryScalarOwnedAsync<T>(connection.CreateCommandWithParameters(sql, parameters, commandType, transaction, commandTimeout), ct);

        /// <summary>
        /// Executes the SQL and returns the first scalar value from the first column, or default if no rows.
        /// Use for primitive/scalar types (int, string, Guid, etc.). Handles DBNull as default(T).
        /// </summary>
        public ValueTask<T?> QueryScalarFirstOrDefaultAsync<T>(
            string sql,
            ReadOnlySpan<SqlParameter> parameters,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
            => QueryScalarFirstOrDefaultOwnedAsync<T>(connection.CreateCommandWithParameters(sql, parameters, commandType, transaction, commandTimeout), ct);

        private SqlCommand CreateCommandWithParameters(
            string sql,
            ReadOnlySpan<SqlParameter> parameters,
            CommandType commandType,
            SqlTransaction? transaction,
            int? commandTimeout)
        {
            var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);

            try
            {
                foreach (var parameter in parameters) command.Parameters.Add(parameter);
            }
            catch
            {
                command.Dispose();
                throw;
            }

            return command;
        }
    }
}
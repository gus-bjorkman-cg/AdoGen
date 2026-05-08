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
public static class SqlConnectionExtensions
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
        /// Updates a database record.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="ct"></param>
        /// <param name="transaction"></param>
        /// <param name="commandTimeout"></param>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<int> UpdateAsync<T>(T model, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
            where T : ISqlDomainModel<T>
            => T.UpdateAsync(model, connection, ct, transaction, commandTimeout);

        /// <summary>
        /// Inserts or updates a database record.
        /// </summary>
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
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
            
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
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
            
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
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
            
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            
            foreach (var parameter in parameters) command.Parameters.Add(parameter);
            
            return await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
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
        public async ValueTask<T?> ExecuteScalarAsync<T>(
            string sql,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
            
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            
            return (T?)await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
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
        public async ValueTask<T?> ExecuteScalarAsync<T>(
            string sql,
            SqlParameter parameter,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
            
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
            command.Parameters.Add(parameter);
            
            return (T?)await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
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
        public async ValueTask<T?> ExecuteScalarAsync<T>(
            string sql,
            IEnumerable<SqlParameter> parameters,
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null,
            int? commandTimeout = null)
        {
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
            
            await using var command = connection.CreateCommand(sql, commandType, transaction, commandTimeout);
                        
            foreach (var parameter in parameters) command.Parameters.Add(parameter);
            
            return (T?)await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
        }
    }
}
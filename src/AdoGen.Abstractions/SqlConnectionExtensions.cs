using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AdoGen.Abstractions;

public static class SqlConnectionExtensions
{
    extension(SqlConnection connection)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask CreateTableAsync<T>(CancellationToken ct, SqlTransaction? transaction = null)
            where T : ISqlDomainModel<T> =>
            await T.CreateTableAsync(connection, ct, transaction).ConfigureAwait(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask InsertAsync<T>(T model, CancellationToken ct, SqlTransaction? transaction = null)
            where T : ISqlDomainModel<T> =>
            await T.InsertAsync(model, connection, ct, transaction).ConfigureAwait(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask InsertAsync<T>(List<T> models, CancellationToken ct, SqlTransaction? transaction = null)
            where T : ISqlDomainModel<T> =>
            await T.InsertAsync(models, connection, ct, transaction).ConfigureAwait(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask UpdateAsync<T>(T model, CancellationToken ct, SqlTransaction? transaction = null)
            where T : ISqlDomainModel<T> =>
            await T.UpdateAsync(model, connection, ct, transaction).ConfigureAwait(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask UpsertAsync<T>(T model, CancellationToken ct, SqlTransaction? transaction = null)
            where T : ISqlDomainModel<T> =>
            await T.UpsertAsync(model, connection, ct, transaction).ConfigureAwait(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask DeleteAsync<T>(T model, CancellationToken ct, SqlTransaction? transaction = null)
            where T : ISqlDomainModel<T> =>
            await T.DeleteAsync(model, connection, ct, transaction).ConfigureAwait(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask TruncateAsync<T>(CancellationToken ct, SqlTransaction? transaction = null)
            where T : ISqlDomainModel<T> =>
            await T.TruncateAsync(connection, ct, transaction).ConfigureAwait(false);

        public async ValueTask<List<T>> QueryAsync<T>(
            string sql, 
            CancellationToken ct, 
            CommandType commandType = CommandType.Text, 
            SqlTransaction? transaction = null) 
            where T : ISqlResult<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction);
            return await command.QueryAsync<T>(ct);
        }
        
        public async ValueTask<List<T>> QueryAsync<T>(
            string sql, 
            SqlParameter parameter, 
            CancellationToken ct, 
            CommandType commandType = CommandType.Text, 
            SqlTransaction? transaction = null) 
            where T : ISqlResult<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction);
            command.Parameters.Add(parameter);
            return await command.QueryAsync<T>(ct);
        }
        
        public async ValueTask<List<T>> QueryAsync<T>(
            string sql, 
            IEnumerable<SqlParameter> parameters, 
            CancellationToken ct, 
            CommandType commandType = CommandType.Text, 
            SqlTransaction? transaction = null) 
            where T : ISqlResult<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction);
            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }
            return await command.QueryAsync<T>(ct);
        }

        public async ValueTask<T?> QueryFirstOrDefaultAsync<T>(
            string sql, 
            CancellationToken ct, 
            CommandType commandType = CommandType.Text, 
            SqlTransaction? transaction = null)
            where T : ISqlResult<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction);
            return await command.QueryFirstOrDefaultAsync<T>(ct);
        }
        
        public async ValueTask<T?> QueryFirstOrDefaultAsync<T>(
            string sql, 
            SqlParameter parameter, 
            CancellationToken ct, 
            CommandType commandType = CommandType.Text, 
            SqlTransaction? transaction = null) 
            where T : ISqlResult<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction);
            command.Parameters.Add(parameter);
            return await command.QueryFirstOrDefaultAsync<T>(ct);
        }
        
        public async ValueTask<T?> QueryFirstOrDefaultAsync<T>(
            string sql,
            IEnumerable<SqlParameter> parameters, 
            CancellationToken ct, 
            CommandType commandType = CommandType.Text, 
            SqlTransaction? transaction = null) 
            where T : ISqlResult<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction);
            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }
            return await command.QueryFirstOrDefaultAsync<T>(ct);
        }

        public async ValueTask<SqlDataReader> QueryMultiAsync(
            string sql, 
            CancellationToken ct, 
            CommandType commandType = CommandType.Text, 
            SqlTransaction? transaction = null)
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction);
            return await command.QueryMultiAsync(ct);
        }
        
        public async ValueTask<SqlDataReader> QueryMultiAsync(
            string sql, 
            SqlParameter parameter, 
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null)
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction);
            command.Parameters.Add(parameter);
            return await command.QueryMultiAsync(ct);
        }
        
        public async ValueTask<SqlDataReader> QueryMultiAsync(
            string sql, 
            IEnumerable<SqlParameter> parameters, 
            CancellationToken ct,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null)
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction);
            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }
            return await command.QueryMultiAsync(ct);
        }
        
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
    }
}
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AdoGen.Abstractions;

public static class SqlConnectionExtensions
{
    extension(SqlConnection connection)
    {
        public async ValueTask CreateTable<T>(CancellationToken ct, SqlTransaction? transaction = null)
            where T : ISqlDomainModel<T>
        {
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
        
            await T.CreateTable(connection, ct, transaction).ConfigureAwait(false);
        }
        
        public async ValueTask Insert<T>(T model, CancellationToken ct, SqlTransaction? transaction = null)
            where T : ISqlDomainModel<T>
        {
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
        
            await T.Insert(model, connection, ct, transaction).ConfigureAwait(false);
        }
        
        public async ValueTask Insert<T>(List<T> models, CancellationToken ct, SqlTransaction? transaction = null)
            where T : ISqlDomainModel<T>
        {
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
        
            await T.Insert(models, connection, ct, transaction).ConfigureAwait(false);
        }
        
        public async ValueTask Update<T>(T model, CancellationToken ct, SqlTransaction? transaction = null)
            where T : ISqlDomainModel<T>
        {
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
        
            await T.Update(model, connection, ct, transaction).ConfigureAwait(false);
        }
                
        public async ValueTask Upsert<T>(T model, CancellationToken ct, SqlTransaction? transaction = null)
            where T : ISqlDomainModel<T>
        {
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
        
            await T.Upsert(model, connection, ct, transaction).ConfigureAwait(false);
        }
        
        public async ValueTask Delete<T>(T model, CancellationToken ct, SqlTransaction? transaction = null)
            where T : ISqlDomainModel<T>
        {
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
        
            await T.Delete(model, connection, ct, transaction).ConfigureAwait(false);
        }
        
        public async ValueTask Truncate<T>(CancellationToken ct, SqlTransaction? transaction = null)
            where T : ISqlDomainModel<T>
        {
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
        
            await T.Truncate(connection, ct, transaction).ConfigureAwait(false);
        }
        
        public async ValueTask<List<T>> QueryAsync<T>(string sql, CancellationToken ct, CommandType commandType = CommandType.Text, SqlTransaction? transaction = null) where T : ISqlResult<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction);
            return await command.Query<T>(ct);
        }
        
        public async ValueTask<List<T>> QueryAsync<T>(string sql, SqlParameter parameter, CancellationToken ct, CommandType commandType = CommandType.Text, SqlTransaction? transaction = null) where T : ISqlResult<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction);
            command.Parameters.Add(parameter);
            return await command.Query<T>(ct);
        }
        
        public async ValueTask<List<T>> QueryAsync<T>(string sql, IEnumerable<SqlParameter> parameters, CancellationToken ct, CommandType commandType = CommandType.Text, SqlTransaction? transaction = null) where T : ISqlResult<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction);
            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }
            return await command.Query<T>(ct);
        }

        public async ValueTask<T?> QueryFirstOrDefaultAsync<T>(string sql, CancellationToken ct, CommandType commandType = CommandType.Text, SqlTransaction? transaction = null) where T : ISqlResult<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction);
            return await command.QueryFirstOrDefault<T>(ct);
        }
        
        public async ValueTask<T?> QueryFirstOrDefaultAsync<T>(string sql, SqlParameter parameter, CancellationToken ct, CommandType commandType = CommandType.Text, SqlTransaction? transaction = null) where T : ISqlResult<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction);
            command.Parameters.Add(parameter);
            return await command.QueryFirstOrDefault<T>(ct);
        }
        
        public async ValueTask<T?> QueryFirstOrDefaultAsync<T>(string sql, IEnumerable<SqlParameter> parameters, CancellationToken ct, CommandType commandType = CommandType.Text, SqlTransaction? transaction = null) where T : ISqlResult<T>
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction);
            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }
            return await command.QueryFirstOrDefault<T>(ct);
        }

        public async ValueTask<SqlDataReader> QueryMultiAsync(string sql, CancellationToken ct, CommandType commandType = CommandType.Text, SqlTransaction? transaction = null)
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction);
            return await command.QueryMulti(ct);
        }
        
        public async ValueTask<SqlDataReader> QueryMultiAsync(string sql, SqlParameter parameter, CancellationToken ct, CommandType commandType = CommandType.Text, SqlTransaction? transaction = null)
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction);
            command.Parameters.Add(parameter);
            return await command.QueryMulti(ct);
        }
        
        public async ValueTask<SqlDataReader> QueryMultiAsync(string sql, IEnumerable<SqlParameter> parameters, CancellationToken ct, CommandType commandType = CommandType.Text, SqlTransaction? transaction = null)
        {
            await using var command = connection.CreateCommand(sql, commandType, transaction);
            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }
            return await command.QueryMulti(ct);
        }
        
        public SqlCommand CreateCommand(
            string sql,
            CommandType commandType = CommandType.Text,
            SqlTransaction? transaction = null)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = commandType;
            
            if (transaction != null) command.Transaction = transaction;
            
            return command;
        }
    }
}
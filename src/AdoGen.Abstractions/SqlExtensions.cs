using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AdoGen.Abstractions;

public interface ISqlResult;

public interface ISqlResult<out T> where T : ISqlResult<T>
{
    static abstract T Map(SqlDataReader reader);
}

public interface ISqlDomainModel : ISqlResult;

public interface ISqlDomainModel<T> where T : ISqlDomainModel<T>
{
    static abstract ValueTask CreateTable(SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
    static abstract ValueTask Insert(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
    static abstract ValueTask Insert(List<T> models, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
    static abstract ValueTask Update(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
    static abstract ValueTask Upsert(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
    static abstract ValueTask Delete(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
    static abstract ValueTask Truncate(SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
}

public static class SqlExtensions
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
    }
    
    extension(SqlCommand command)
    {
        public async ValueTask<List<T>> Query<T>(CancellationToken ct)
            where T : ISqlResult<T>
        {
            if (command.Connection.State != ConnectionState.Open) await command.Connection.OpenAsync(ct).ConfigureAwait(false);

            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult, ct).ConfigureAwait(false);
        
            if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return [];
            
            var items = new List<T>();
            do items.Add(reader.Map<T>()); while (await reader.ReadAsync(ct).ConfigureAwait(false));

            return items;
        }

        public async ValueTask<T?> QueryFirstOrDefault<T>(CancellationToken ct)
            where T : ISqlResult<T>
        {
            if (command.Connection.State != ConnectionState.Open) await command.Connection.OpenAsync(ct).ConfigureAwait(false);

            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SingleRow, ct).ConfigureAwait(false);
        
            if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return default;
        
            return reader.Map<T>();
        }

        public async ValueTask<SqlDataReader> QueryMulti(CancellationToken ct)
        {
            if (command.Connection.State != ConnectionState.Open) await command.Connection.OpenAsync(ct).ConfigureAwait(false);
        
            return await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        }
    }

    extension(SqlCommand cmd)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CreateParameter(string name, 
            object? value, 
            SqlDbType type,
            int size = 0)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.SqlDbType = type;
            p.SqlValue = value;
        
            if (size > 0) p.Size = size;
        
            cmd.Parameters.Add(p);
        }
    }

    extension(SqlConnection connection)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    extension(SqlDataReader reader)
    {
        public async ValueTask<List<T>> QueryAsync<T>(CancellationToken ct)
            where T : ISqlResult<T>
        {
            if (!await reader.ReadAsync(ct))
            {
                await reader.NextResultAsync(ct);
                return [];
            }
        
            var items = new List<T>();
            do items.Add(reader.Map<T>()); while (await reader.ReadAsync(ct));

            await reader.NextResultAsync(ct);
            return items;
        }

        public async ValueTask<T?> QueryFirstOrDefaultAsync<T>(CancellationToken ct)
            where T : ISqlResult<T>
        {
            if (!await reader.ReadAsync(ct))
            {
                await reader.NextResultAsync(ct);
                return default;
            }
        
            var item = reader.Map<T>();
        
            await reader.NextResultAsync(ct);
            return item;
        }
    }

    extension(SqlDataReader reader)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Map<T>() where T : ISqlResult<T> => T.Map(reader);
    }
}
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AdoGen.Abstractions;

public static class SqlCommandExtensions
{
    extension(SqlCommand command)
    {
        public async ValueTask<List<T>> QueryAsync<T>(CancellationToken ct)
            where T : ISqlResult<T>
        {
            if (command.Connection.State != ConnectionState.Open) await command.Connection.OpenAsync(ct).ConfigureAwait(false);

            await using var reader = await command
                .ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult, ct).ConfigureAwait(false);
        
            if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return [];
            
            var items = new List<T>();
            do items.Add(reader.Map<T>()); while (await reader.ReadAsync(ct).ConfigureAwait(false));

            return items;
        }

        public async ValueTask<T?> QueryFirstOrDefaultAsync<T>(CancellationToken ct)
            where T : ISqlResult<T>
        {
            if (command.Connection.State != ConnectionState.Open) await command.Connection.OpenAsync(ct).ConfigureAwait(false);

            await using var reader = await command
                .ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SingleRow, ct).ConfigureAwait(false);
        
            if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return default;
        
            return reader.Map<T>();
        }

        public async ValueTask<SqlDataReader> QueryMultiAsync(CancellationToken ct)
        {
            if (command.Connection.State != ConnectionState.Open) await command.Connection.OpenAsync(ct).ConfigureAwait(false);
        
            return await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        }
        
        public void CreateParameter(string name, 
            object? value, 
            SqlDbType type,
            int size = 0)
        {
            var p = command.CreateParameter();
            p.ParameterName = name;
            p.SqlDbType = type;
            p.SqlValue = value;
        
            if (size > 0) p.Size = size;
        
            command.Parameters.Add(p);
        }
    }
}
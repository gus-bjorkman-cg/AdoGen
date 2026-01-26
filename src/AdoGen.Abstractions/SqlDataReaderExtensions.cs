using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AdoGen.Abstractions;

public static class SqlDataReaderExtensions
{
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
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Map<T>() where T : ISqlResult<T> => T.Map(reader);
    }
}
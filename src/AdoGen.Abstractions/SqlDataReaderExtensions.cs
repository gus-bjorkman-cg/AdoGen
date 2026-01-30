using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AdoGen.Abstractions;

/// <summary>
/// Extensions class for SqlDataReader.
/// </summary>
public static class SqlDataReaderExtensions
{
    extension(SqlDataReader reader)
    {
        /// <summary>
        /// Gets the result from the reader and maps it into objects by using the source generated mapper.
        /// </summary>
        /// <param name="ct"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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

        /// <summary>
        /// Gets the result from the reader and maps it into an object by using the source generated mapper.
        /// </summary>
        /// <param name="ct"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>Null or the mapped object from the database</returns>
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
        
        /// <summary>
        /// Maps the objects by using the source generated mapper.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Map<T>() where T : ISqlResult<T> => T.Map(reader);
    }
}
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AdoGen.Abstractions;

public interface ISingleIdModel<TModel, TKey>
    where TModel : ISingleIdModel<TModel, TKey>
{
    static abstract ValueTask<int> DeleteAsync(
        SqlConnection connection,
        List<TKey> ids,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null);
}

public static class SqlConnectionSingleIdExtensions
{
    public static async ValueTask<int> DeleteAsync<TModel, TKey>(
        this SqlConnection connection,
        List<TKey> ids,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : ISingleIdModel<TModel, TKey>
        => await TModel.DeleteAsync(connection, ids, ct, transaction, commandTimeout);
}
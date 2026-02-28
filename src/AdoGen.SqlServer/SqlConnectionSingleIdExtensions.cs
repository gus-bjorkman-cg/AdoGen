using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AdoGen.SqlServer;

/// <summary>
/// Interface used by AdoGen to generate delete operations for models with a single key.
/// The generated code will implement this interface and the extension method below will call the generated code.
/// </summary>
/// <typeparam name="TModel"></typeparam>
/// <typeparam name="TKey"></typeparam>
public interface ISqlSingleIdModel<TModel, TKey>
    where TModel : ISqlSingleIdModel<TModel, TKey>
{
    /// <summary>
    /// Deletes the records with the given ids.
    /// The generated code will create a SQL statement with an IN clause to delete all the records in one roundtrip.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="ids"></param>
    /// <param name="ct"></param>
    /// <param name="transaction"></param>
    /// <param name="commandTimeout"></param>
    /// <returns></returns>
    static abstract ValueTask<int> DeleteAsync(
        SqlConnection connection,
        List<TKey> ids,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null);
}

/// <summary>
/// Extensions for ISingleIdModel to call the generated delete code.
/// </summary>
public static class SqlConnectionSingleIdExtensions
{
    /// <summary>
    /// Deletes the records with the given ids.
    /// The generated code will create a SQL statement with an IN clause to delete all the records in one roundtrip.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="ids"></param>
    /// <param name="ct"></param>
    /// <param name="transaction"></param>
    /// <param name="commandTimeout"></param>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <returns></returns>
    public static async ValueTask<int> DeleteAsync<TModel, TKey>(
        this SqlConnection connection,
        List<TKey> ids,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : ISqlSingleIdModel<TModel, TKey>
        => await TModel.DeleteAsync(connection, ids, ct, transaction, commandTimeout);
}
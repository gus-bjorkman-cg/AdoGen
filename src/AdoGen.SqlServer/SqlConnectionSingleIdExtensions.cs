using System;
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
    static abstract ValueTask<int> DeleteAsync(
        SqlConnection connection,
        List<TKey> ids,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null);

    /// <summary>
    /// Returns true if a record with the given id exists in the database.
    /// The generated code uses SELECT 1 … WHERE pk = @id via ExecuteScalarAsync.
    /// </summary>
    static abstract ValueTask<bool> ExistsAsync(
        SqlConnection connection,
        TKey id,
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
    /// <returns></returns>
    public static async ValueTask<int> DeleteAsync<TModel>(
        this SqlConnection connection,
        List<Guid> ids,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : ISqlSingleIdModel<TModel, Guid>
        => await TModel.DeleteAsync(connection, ids, ct, transaction, commandTimeout);
    
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
    /// <returns></returns>
    public static async ValueTask<int> DeleteAsync<TModel>(
        this SqlConnection connection,
        List<long> ids,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : ISqlSingleIdModel<TModel, long>
        => await TModel.DeleteAsync(connection, ids, ct, transaction, commandTimeout);
    
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
    /// <returns></returns>
    public static async ValueTask<int> DeleteAsync<TModel>(
        this SqlConnection connection,
        List<int> ids,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : ISqlSingleIdModel<TModel, int>
        => await TModel.DeleteAsync(connection, ids, ct, transaction, commandTimeout);
    
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
    /// <returns></returns>
    public static async ValueTask<int> DeleteAsync<TModel>(
        this SqlConnection connection,
        List<short> ids,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : ISqlSingleIdModel<TModel, short>
        => await TModel.DeleteAsync(connection, ids, ct, transaction, commandTimeout);
    
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
    /// <returns></returns>
    public static async ValueTask<int> DeleteAsync<TModel>(
        this SqlConnection connection,
        List<decimal> ids,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : ISqlSingleIdModel<TModel, decimal>
        => await TModel.DeleteAsync(connection, ids, ct, transaction, commandTimeout);
    
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
    /// <returns></returns>
    public static async ValueTask<int> DeleteAsync<TModel>(
        this SqlConnection connection,
        List<string> ids,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : ISqlSingleIdModel<TModel, string>
        => await TModel.DeleteAsync(connection, ids, ct, transaction, commandTimeout);

    /// <summary>
    /// Returns true if a record with the given id exists in the database.
    /// </summary>
    public static ValueTask<bool> ExistsAsync<TModel, TKey>(
        this SqlConnection connection,
        TKey id,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : ISqlSingleIdModel<TModel, TKey>
        => TModel.ExistsAsync(connection, id, ct, transaction, commandTimeout);
    
        /// <summary>
    /// Returns true if a record with the given id exists in the database.
    /// </summary>
    public static ValueTask<bool> ExistsAsync<TModel>(
        this SqlConnection connection,
        Guid id,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : ISqlSingleIdModel<TModel, Guid>
        => TModel.ExistsAsync(connection, id, ct, transaction, commandTimeout);
    
    /// <summary>
    /// Returns true if a record with the given id exists in the database.
    /// </summary>
    public static ValueTask<bool> ExistsAsync<TModel>(
        this SqlConnection connection,
        long id,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : ISqlSingleIdModel<TModel, long>
        => TModel.ExistsAsync(connection, id, ct, transaction, commandTimeout);
    
    /// <summary>
    /// Returns true if a record with the given id exists in the database.
    /// </summary>
    public static ValueTask<bool> ExistsAsync<TModel>(
        this SqlConnection connection,
        int id,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : ISqlSingleIdModel<TModel, int>
        => TModel.ExistsAsync(connection, id, ct, transaction, commandTimeout);
    
    /// <summary>
    /// Returns true if a record with the given id exists in the database.
    /// </summary>
    public static ValueTask<bool> ExistsAsync<TModel>(
        this SqlConnection connection,
        short id,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : ISqlSingleIdModel<TModel, short>
        => TModel.ExistsAsync(connection, id, ct, transaction, commandTimeout);
    
    /// <summary>
    /// Returns true if a record with the given id exists in the database.
    /// </summary>
    public static ValueTask<bool> ExistsAsync<TModel>(
        this SqlConnection connection,
        string id,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : ISqlSingleIdModel<TModel, string>
        => TModel.ExistsAsync(connection, id, ct, transaction, commandTimeout);
}
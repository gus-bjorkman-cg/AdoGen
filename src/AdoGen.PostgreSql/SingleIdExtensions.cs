using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace AdoGen.PostgreSql;

/// <summary>
/// Interface used by AdoGen to generate delete operations for models with a single key.
/// The generated code will implement this interface and the extension method below will call the generated code.
/// </summary>
public interface INpgsqlSingleIdModel<TModel, TKey>
    where TModel : INpgsqlSingleIdModel<TModel, TKey>
{
    /// <summary>
    /// Deletes the records with the given ids.
    /// The generated code will create a SQL statement with an IN clause to delete all the records in one roundtrip.
    /// </summary>
    static abstract ValueTask<int> DeleteAsync(
        NpgsqlConnection connection,
        List<TKey> ids,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null);

    /// <summary>
    /// Returns true if a record with the given id exists in the database.
    /// The generated code uses SELECT 1 … WHERE pk = @id via ExecuteScalarAsync.
    /// </summary>
    static abstract ValueTask<bool> ExistsAsync(
        NpgsqlConnection connection,
        TKey id,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null);
}

/// <summary>
/// Extensions for ISingleIdModel to call the generated delete code.
/// </summary>
public static class NpgsqlConnectionSingleIdExtensions
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
        this NpgsqlConnection connection,
        List<TKey> ids,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : INpgsqlSingleIdModel<TModel, TKey>
        => await TModel.DeleteAsync(connection, ids, ct, transaction, commandTimeout).ConfigureAwait(false);
    
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
    public static ValueTask<int> DeleteAsync<TModel>(
        this NpgsqlConnection connection,
        List<Guid> ids,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : INpgsqlSingleIdModel<TModel, Guid>
        => TModel.DeleteAsync(connection, ids, ct, transaction, commandTimeout);
    
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
    public static ValueTask<int> DeleteAsync<TModel>(
        this NpgsqlConnection connection,
        List<long> ids,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : INpgsqlSingleIdModel<TModel, long>
        => TModel.DeleteAsync(connection, ids, ct, transaction, commandTimeout);
    
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
    public static ValueTask<int> DeleteAsync<TModel>(
        this NpgsqlConnection connection,
        List<int> ids,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : INpgsqlSingleIdModel<TModel, int>
        => TModel.DeleteAsync(connection, ids, ct, transaction, commandTimeout);
    
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
    public static ValueTask<int> DeleteAsync<TModel>(
        this NpgsqlConnection connection,
        List<short> ids,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : INpgsqlSingleIdModel<TModel, short>
        => TModel.DeleteAsync(connection, ids, ct, transaction, commandTimeout);
    
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
    public static ValueTask<int> DeleteAsync<TModel>(
        this NpgsqlConnection connection,
        List<decimal> ids,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : INpgsqlSingleIdModel<TModel, decimal>
        => TModel.DeleteAsync(connection, ids, ct, transaction, commandTimeout);
    
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
    public static ValueTask<int> DeleteAsync<TModel>(
        this NpgsqlConnection connection,
        List<string> ids,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : INpgsqlSingleIdModel<TModel, string>
        => TModel.DeleteAsync(connection, ids, ct, transaction, commandTimeout);

    /// <summary>
    /// Returns true if a record with the given id exists in the database.
    /// </summary>
    public static ValueTask<bool> ExistsAsync<TModel, TKey>(
        this NpgsqlConnection connection,
        TKey id,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : INpgsqlSingleIdModel<TModel, TKey>
        => TModel.ExistsAsync(connection, id, ct, transaction, commandTimeout);
    
    /// <summary>
    /// Returns true if a record with the given id exists in the database.
    /// </summary>
    public static ValueTask<bool> ExistsAsync<TModel>(
        this NpgsqlConnection connection,
        Guid id,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : INpgsqlSingleIdModel<TModel, Guid>
        => TModel.ExistsAsync(connection, id, ct, transaction, commandTimeout);
    
    /// <summary>
    /// Returns true if a record with the given id exists in the database.
    /// </summary>
    public static ValueTask<bool> ExistsAsync<TModel>(
        this NpgsqlConnection connection,
        long id,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : INpgsqlSingleIdModel<TModel, long>
        => TModel.ExistsAsync(connection, id, ct, transaction, commandTimeout);
    
    /// <summary>
    /// Returns true if a record with the given id exists in the database.
    /// </summary>
    public static ValueTask<bool> ExistsAsync<TModel>(
        this NpgsqlConnection connection,
        int id,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : INpgsqlSingleIdModel<TModel, int>
        => TModel.ExistsAsync(connection, id, ct, transaction, commandTimeout);
    
    /// <summary>
    /// Returns true if a record with the given id exists in the database.
    /// </summary>
    public static ValueTask<bool> ExistsAsync<TModel>(
        this NpgsqlConnection connection,
        short id,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : INpgsqlSingleIdModel<TModel, short>
        => TModel.ExistsAsync(connection, id, ct, transaction, commandTimeout);
    
    /// <summary>
    /// Returns true if a record with the given id exists in the database.
    /// </summary>
    public static ValueTask<bool> ExistsAsync<TModel>(
        this NpgsqlConnection connection,
        string id,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : INpgsqlSingleIdModel<TModel, string>
        => TModel.ExistsAsync(connection, id, ct, transaction, commandTimeout);
}

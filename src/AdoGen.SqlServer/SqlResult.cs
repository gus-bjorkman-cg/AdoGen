using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AdoGen.SqlServer;

/// <summary>
/// Interface used to generate ado gen mapper and sql helper class.
/// </summary>
public interface ISqlResult;

/// <summary>
/// Interface used by ado gen to make mapping extension to work.
/// </summary>
public interface ISqlResult<out T> where T : ISqlResult<T>
{
    /// <summary>
    /// Maps the objects by using the source generated mapper.
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    static abstract T Map(SqlDataReader reader);
}

/// <summary>
/// Interface used to generate ado gen domain operations class.
/// </summary>
public interface ISqlDomainModel : ISqlResult;

/// <summary>
/// Interface used by ado gen to make domain operations class to work.
/// </summary>
public interface ISqlDomainModel<T> where T : ISqlDomainModel<T>
{
    /// <summary>
    /// Creates the database table.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="ct"></param>
    /// <param name="transaction"></param>
    /// <param name="commandTimeout"></param>
    /// <returns></returns>
    static abstract ValueTask CreateTableAsync(SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null);
    
    /// <summary>
    /// Inserts a database record.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="connection"></param>
    /// <param name="ct"></param>
    /// <param name="transaction"></param>
    /// <param name="commandTimeout"></param>
    /// <returns>Number of affected rows</returns>
    static abstract ValueTask<int> InsertAsync(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null);
    
    /// <summary>
    /// Inserts multiple database records in one roundtrip.
    /// </summary>
    /// <param name="models"></param>
    /// <param name="connection"></param>
    /// <param name="ct"></param>
    /// <param name="transaction"></param>
    /// <param name="commandTimeout"></param>
    /// <returns>Number of affected rows</returns>
    static abstract ValueTask<int> InsertAsync(List<T> models, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null);
    
    /// <summary>
    /// Updates a database record.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="connection"></param>
    /// <param name="ct"></param>
    /// <param name="transaction"></param>
    /// <param name="commandTimeout"></param>
    /// <returns>Number of affected rows</returns>
    static abstract ValueTask<int> UpdateAsync(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null);
    
    /// <summary>
    /// Inserts or updates a database record.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="connection"></param>
    /// <param name="ct"></param>
    /// <param name="transaction"></param>
    /// <param name="commandTimeout"></param>
    /// <returns></returns>
    static abstract ValueTask<int> UpsertAsync(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null);
    
    /// <summary>
    /// Deletes a database record.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="connection"></param>
    /// <param name="ct"></param>
    /// <param name="transaction"></param>
    /// <param name="commandTimeout"></param>
    /// <returns>Number of affected rows</returns>
    static abstract ValueTask<int> DeleteAsync(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null);
    
    /// <summary>
    /// Truncates a database table.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="ct"></param>
    /// <param name="transaction"></param>
    /// <param name="commandTimeout"></param>
    /// <returns>Number of affected rows</returns>
    static abstract ValueTask<int> TruncateAsync(SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null);
}

/// <summary>
/// Struct that represents the result of a bulk apply operation,
/// containing the number of inserted, updated, and deleted records.
/// </summary>
/// <param name="Inserted"></param>
/// <param name="Updated"></param>
/// <param name="Deleted"></param>
public readonly record struct BulkApplyResult(int Inserted, int Updated, int Deleted)
{
    /// <summary>
    /// Static property that represents an empty result.
    /// </summary>
    public static BulkApplyResult Empty { get; } = new(0, 0, 0);
}

/// <summary>
/// Interface used to generate ado gen bulk operations class.
/// </summary>
public interface ISqlBulkModel : ISqlDomainModel;

/// <summary>
/// Interface used by ado gen to make bulk operations class to work.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ISqlBulkModel<T> : ISqlBulkModel, ISqlDomainModel<T> where T : ISqlBulkModel<T>;
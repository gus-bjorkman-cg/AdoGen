using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AdoGen.Abstractions;

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
    /// <returns></returns>
    static abstract ValueTask InsertAsync(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null);
    
    /// <summary>
    /// Inserts multiple database records in one roundtrip.
    /// </summary>
    /// <param name="models"></param>
    /// <param name="connection"></param>
    /// <param name="ct"></param>
    /// <param name="transaction"></param>
    /// <param name="commandTimeout"></param>
    /// <returns></returns>
    static abstract ValueTask InsertAsync(List<T> models, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null);
    
    /// <summary>
    /// Updates a database record.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="connection"></param>
    /// <param name="ct"></param>
    /// <param name="transaction"></param>
    /// <param name="commandTimeout"></param>
    /// <returns></returns>
    static abstract ValueTask UpdateAsync(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null);
    
    /// <summary>
    /// Inserts or updates a database record.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="connection"></param>
    /// <param name="ct"></param>
    /// <param name="transaction"></param>
    /// <param name="commandTimeout"></param>
    /// <returns></returns>
    static abstract ValueTask UpsertAsync(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null);
    
    /// <summary>
    /// Deletes a database record.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="connection"></param>
    /// <param name="ct"></param>
    /// <param name="transaction"></param>
    /// <param name="commandTimeout"></param>
    /// <returns></returns>
    static abstract ValueTask DeleteAsync(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null);
    
    /// <summary>
    /// Truncates a database table.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="ct"></param>
    /// <param name="transaction"></param>
    /// <param name="commandTimeout"></param>
    /// <returns></returns>
    static abstract ValueTask TruncateAsync(SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null);
}

public readonly record struct BulkApplyResult(int Inserted, int Updated, int Deleted);

public interface ISqlBulkModel : ISqlDomainModel;
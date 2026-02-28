using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace AdoGen.PostgreSql;

/// <summary>
/// Interface used to generate AdoGen mapper and SQL helper class for PostgreSQL.
/// </summary>
public interface INpgsqlResult;

/// <summary>
/// Interface used by AdoGen to make mapping extensions work for PostgreSQL.
/// </summary>
public interface INpgsqlResult<out T> where T : INpgsqlResult<T>
{
    /// <summary>
    /// Maps the objects by using the source generated mapper.
    /// </summary>
    static abstract T Map(NpgsqlDataReader reader);
}

/// <summary>
/// Interface used to generate AdoGen domain operations class for PostgreSQL.
/// </summary>
public interface INpgsqlDomainModel : INpgsqlResult;

/// <summary>
/// Interface used by AdoGen to make domain operations class work for PostgreSQL.
/// </summary>
public interface INpgsqlDomainModel<T> where T : INpgsqlDomainModel<T>
{
    /// <summary>
    /// Creates the database table.
    /// </summary>
    static abstract ValueTask CreateTableAsync(NpgsqlConnection connection, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null);

    /// <summary>
    /// Inserts a database record.
    /// </summary>
    static abstract ValueTask<int> InsertAsync(T model, NpgsqlConnection connection, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null);

    /// <summary>
    /// Inserts multiple database records in one roundtrip.
    /// </summary>
    static abstract ValueTask<int> InsertAsync(List<T> models, NpgsqlConnection connection, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null);

    /// <summary>
    /// Updates a database record.
    /// </summary>
    static abstract ValueTask<int> UpdateAsync(T model, NpgsqlConnection connection, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null);

    /// <summary>
    /// Inserts or updates a database record.
    /// </summary>
    static abstract ValueTask<int> UpsertAsync(T model, NpgsqlConnection connection, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null);

    /// <summary>
    /// Deletes a database record.
    /// </summary>
    static abstract ValueTask<int> DeleteAsync(T model, NpgsqlConnection connection, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null);

    /// <summary>
    /// Truncates a database table.
    /// </summary>
    static abstract ValueTask<int> TruncateAsync(NpgsqlConnection connection, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null);
}

/// <summary>
/// Struct that represents the result of a bulk apply operation,
/// containing the number of inserted, updated, and deleted records.
/// </summary>
public readonly record struct BulkApplyResult(int Inserted, int Updated, int Deleted)
{
    /// <summary>
    /// Static property that represents an empty result.
    /// </summary>
    public static BulkApplyResult Empty { get; } = new(0, 0, 0);
}

/// <summary>
/// Interface used to generate AdoGen bulk operations class for PostgreSQL.
/// </summary>
public interface INpgsqlBulkModel : INpgsqlDomainModel;

/// <summary>
/// Interface used by AdoGen to make bulk operations class work for PostgreSQL.
/// </summary>
public interface INpgsqlBulkModel<T> : INpgsqlBulkModel, INpgsqlDomainModel<T> where T : INpgsqlBulkModel<T>;


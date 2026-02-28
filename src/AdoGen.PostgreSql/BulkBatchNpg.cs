using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace AdoGen.PostgreSql;

/// <summary>
/// Represents a batch of bulk operations (insert, update, delete) to be applied to a PostgreSQL
/// database using COPY and custom SQL commands.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class BulkBatchNpg<T> where T : INpgsqlBulkModel<T>
{
    /// <summary>
    /// The list of items to be processed in the batch.
    /// The actual database operations will happen when SaveChangesAsync is called.
    /// </summary>
    public List<T> Items { get; }

    /// <summary>
    /// The list of operations corresponding to each item in the batch.
    /// </summary>
    public List<BulkOp> Operations { get; }

    /// <summary>
    /// The threshold for the number of rows in the batch to decide whether to use the index-based apply SQL.
    /// </summary>
    public int IndexThresholdRows { get; set; } = 500;

    /// <summary>
    /// The batch size for the COPY write operation.
    /// </summary>
    public int BulkCopyBatchSize { get; set; } = 5000;

    /// <summary>
    /// The default timeout in seconds for the bulk copy and apply commands.
    /// Can also be overridden by passing a commandTimeout to SaveChangesAsync.
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Indicates whether the batch contains insert operations.
    /// </summary>
    public bool HasInserts { get; private set; }

    /// <summary>
    /// Indicates whether the batch contains update operations.
    /// </summary>
    public bool HasUpdates { get; private set; }

    /// <summary>
    /// Indicates whether the batch contains delete operations.
    /// </summary>
    public bool HasDeletes { get; private set; }

    /// <summary>
    /// The SQL command to create the temp table for the bulk operation.
    /// Set by generated code.
    /// </summary>
    protected abstract string SqlCreateTempTable { get; }

    /// <summary>
    /// The name of the temp table to be used for the bulk operation.
    /// Set by generated code.
    /// </summary>
    protected abstract string TempTableName { get; }

    /// <summary>
    /// The SQL command to apply the batch of operations using an index on the temp table.
    /// Set by generated code.
    /// </summary>
    protected abstract string SqlApplyWithIndex { get; }

    /// <summary>
    /// The SQL command to apply the batch of operations without using an index on the temp table.
    /// Set by generated code.
    /// </summary>
    protected abstract string SqlApplyNoIndex { get; }

    /// <summary>
    /// The number of fields written per row.
    /// Set by generated code.
    /// </summary>
    protected abstract int FieldCount { get; }

    /// <summary>
    /// Initializes a new instance of the BulkBatch class with an optional initial capacity.
    /// </summary>
    /// <param name="capacity"></param>
    public BulkBatchNpg(int capacity)
    {
        Items = new List<T>(capacity);
        Operations = new List<BulkOp>(capacity);
    }

    /// <summary>
    /// Writes the items to the server. Implemented by generated code.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="transaction"></param>
    /// <param name="ct"></param>
    protected abstract ValueTask WriteItemsToServerAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken ct);

    /// <summary>
    /// Adds the item to the batch with insert operation.
    /// </summary>
    /// <param name="item"></param>
    public void Add(T item) => AddEntity(item, BulkOp.Insert);

    /// <summary>
    /// Adds the items to the batch with insert operation.
    /// </summary>
    /// <param name="items"></param>
    public void AddRange(IEnumerable<T> items) => AddEntityRange(items, BulkOp.Insert);

    /// <summary>
    /// Adds the item to the batch with update operation.
    /// </summary>
    /// <param name="item"></param>
    public void Update(T item) => AddEntity(item, BulkOp.Update);

    /// <summary>
    /// Adds the items to the batch with update operation.
    /// </summary>
    /// <param name="items"></param>
    public void UpdateRange(IEnumerable<T> items) => AddEntityRange(items, BulkOp.Update);

    /// <summary>
    /// Adds the item to the batch with delete operation.
    /// </summary>
    /// <param name="item"></param>
    public void Remove(T item) => AddEntity(item, BulkOp.Delete);

    /// <summary>
    /// Adds the items to the batch with delete operation.
    /// </summary>
    /// <param name="items"></param>
    public void RemoveRange(IEnumerable<T> items) => AddEntityRange(items, BulkOp.Delete);

    private void AddEntity(T item, BulkOp operation)
    {
        Items.Add(item);
        Operations.Add(operation);

        if (operation == BulkOp.Insert) HasInserts = true;
        else if (operation == BulkOp.Update) HasUpdates = true;
        else if (operation == BulkOp.Delete) HasDeletes = true;
    }

    private void AddEntityRange(IEnumerable<T> items, BulkOp operation)
    {
        foreach (var item in items)
        {
            Items.Add(item);
            Operations.Add(operation);
        }

        if (operation == BulkOp.Insert) HasInserts = true;
        else if (operation == BulkOp.Update) HasUpdates = true;
        else if (operation == BulkOp.Delete) HasDeletes = true;
    }

    /// <summary>
    /// Applies the batch of operations to the database.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="transaction"></param>
    /// <param name="ct"></param>
    /// <param name="commandTimeout"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async ValueTask<BulkApplyResult> SaveChangesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct,
        int? commandTimeout = null)
    {
        if (Items.Count == 0) return BulkApplyResult.Empty;
        if (transaction is null) throw new ArgumentNullException(nameof(transaction));
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);

        // Fast-path: if we're only inserting and can fit it in a single statement, generated InsertAsync(List<T>) can handle it.
        if (HasInserts && !HasUpdates && !HasDeletes)
        {
            // PostgreSQL doesn't have the 2100 parameter limit, but very large parameter sets are still costly.
            // We keep this heuristic as a safety valve.
            var parameterCount = Items.Count * FieldCount;
            if (parameterCount < 10_000)
            {
                var inserted = await T.InsertAsync(Items, connection, ct, transaction, commandTimeout).ConfigureAwait(false);
                return new BulkApplyResult(inserted, 0, 0);
            }
        }

        await using (var create = connection.CreateCommand(SqlCreateTempTable, CommandType.Text, transaction, commandTimeout))
        {
            await create.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        await WriteItemsToServerAsync(connection, transaction, ct).ConfigureAwait(false);

        var sql = Items.Count >= IndexThresholdRows ? SqlApplyWithIndex : SqlApplyNoIndex;
        await using var cmd = connection.CreateCommand(sql, CommandType.Text, transaction, commandTimeout);
        await using var resultReader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

        if (!await resultReader.ReadAsync(ct).ConfigureAwait(false)) return BulkApplyResult.Empty;

        return new BulkApplyResult(
            Inserted: resultReader.GetInt32(0),
            Updated: resultReader.GetInt32(1),
            Deleted: resultReader.GetInt32(2));
    }

    /// <summary>
    /// Clears the batch.
    /// </summary>
    public void Clear()
    {
        Items.Clear();
        Operations.Clear();
        HasInserts = false;
        HasUpdates = false;
        HasDeletes = false;
    }
}

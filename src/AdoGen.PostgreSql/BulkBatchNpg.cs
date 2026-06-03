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
    /// Max number of parameters allowed for insert operations before falling back to the COPY approach.
    /// Only applicable for insert only operation in batch.
    /// </summary>
    public int ParameterThreshold { get; set; } = 3_000;
    
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
    /// Indicates whether the batch contains upsert operations.
    /// </summary>
    public bool HasUpserts { get; private set; }

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
    /// The SQL command to create the staging index after data is loaded.
    /// Set by generated code.
    /// </summary>
    protected abstract string SqlCreateIndex { get; }

    /// <summary>
    /// The SQL ANALYZE statement for the temp table. Executed when rowCount > 1000.
    /// Set by generated code.
    /// </summary>
    protected abstract string SqlAnalyze { get; }

    /// <summary>UPDATE for 'U' rows. Null when the DTO has no non-key writable columns.</summary>
    protected abstract string? SqlUpdateU { get; }

    /// <summary>INSERT for 'I' rows. Null when the DTO has no writable columns.</summary>
    protected abstract string? SqlInsertI { get; }

    /// <summary>DELETE for 'D' rows. Always present.</summary>
    protected abstract string SqlDeleteD { get; }

    /// <summary>UPDATE for 'M' rows (upsert update pass). Null when no non-key writable columns.</summary>
    protected abstract string? SqlUpdateM { get; }

    /// <summary>INSERT for 'M' rows using ON CONFLICT DO NOTHING. Null when no writable columns or no conflict keys.</summary>
    protected abstract string? SqlInsertM { get; }

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
    /// <param name="ct"></param>
    protected abstract ValueTask WriteItemsToServerAsync(NpgsqlConnection connection, CancellationToken ct);

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

    /// <summary>
    /// Adds the item to the batch with upsert (insert-or-update) operation.
    /// </summary>
    /// <param name="item"></param>
    public void Upsert(T item) => AddEntity(item, BulkOp.Upsert);

    /// <summary>
    /// Adds the items to the batch with upsert (insert-or-update) operation.
    /// </summary>
    /// <param name="items"></param>
    public void UpsertRange(IEnumerable<T> items) => AddEntityRange(items, BulkOp.Upsert);

    private void AddEntity(T item, BulkOp operation)
    {
        Items.Add(item);
        Operations.Add(operation);

        if (operation == BulkOp.Insert) HasInserts = true;
        else if (operation == BulkOp.Update) HasUpdates = true;
        else if (operation == BulkOp.Delete) HasDeletes = true;
        else if (operation == BulkOp.Upsert) HasUpserts = true;
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
        else if (operation == BulkOp.Upsert) HasUpserts = true;
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
        ArgumentNullException.ThrowIfNull(transaction);
        
        if (Items.Count == 0) return BulkApplyResult.Empty;
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
        
        if (HasInserts && !HasUpdates && !HasDeletes && !HasUpserts)
        {
            var parameterCount = Items.Count * FieldCount;
            
            if (parameterCount < ParameterThreshold)
            {
                var inserted = await T.InsertAsync(Items, connection, ct, transaction, commandTimeout).ConfigureAwait(false);
                return new BulkApplyResult(inserted, 0, 0);
            }
        }

        await using var createCmd =
            connection.CreateCommand(SqlCreateTempTable, CommandType.Text, transaction, commandTimeout);
        await createCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

        await WriteItemsToServerAsync(connection, ct).ConfigureAwait(false);

        // All post-COPY work in a single NpgsqlBatch — one round trip.
        await using var batch = new NpgsqlBatch(connection, transaction);

        // Staging index after COPY — no IF NOT EXISTS; fresh temp table every time
        batch.BatchCommands.Add(new NpgsqlBatchCommand(SqlCreateIndex));

        // ANALYZE when batch is large enough for the planner to benefit
        if (Items.Count > 1000)
            batch.BatchCommands.Add(new NpgsqlBatchCommand(SqlAnalyze));

        // Track DML command indices so we can read RecordsAffected per operation
        var updateUIdx = AddIfNotNull(batch, SqlUpdateU);
        var insertIIdx = AddIfNotNull(batch, SqlInsertI);
        var deleteDIdx = AddCmd(batch, SqlDeleteD);
        var updateMIdx = AddIfNotNull(batch, SqlUpdateM);
        var insertMIdx = AddIfNotNull(batch, SqlInsertM);

        await batch.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

        var totalInserted = 0;
        var totalUpdated  = 0;
        var totalDeleted  = 0;

        if (updateUIdx >= 0) totalUpdated  += batch.BatchCommands[updateUIdx].RecordsAffected;
        if (insertIIdx >= 0) totalInserted += batch.BatchCommands[insertIIdx].RecordsAffected;
                             totalDeleted  += batch.BatchCommands[deleteDIdx].RecordsAffected;
        if (updateMIdx >= 0) totalUpdated  += batch.BatchCommands[updateMIdx].RecordsAffected;
        if (insertMIdx >= 0) totalInserted += batch.BatchCommands[insertMIdx].RecordsAffected;

        return new BulkApplyResult(totalInserted, totalUpdated, totalDeleted);
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
        HasUpserts = false;
    }

    private static int AddCmd(NpgsqlBatch batch, string sql)
    {
        batch.BatchCommands.Add(new NpgsqlBatchCommand(sql));
        return batch.BatchCommands.Count - 1;
    }

    private static int AddIfNotNull(NpgsqlBatch batch, string? sql)
        => sql is not null ? AddCmd(batch, sql) : -1;
}

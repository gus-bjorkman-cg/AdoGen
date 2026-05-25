using System.Threading;
using Microsoft.Data.SqlClient;

namespace AdoGen.SqlServer;

/// <summary>
/// Extension methods on <see cref="SqlBatch"/> for adding AdoGen-managed commands.
/// Mix with your own <see cref="SqlBatchCommand"/> instances freely — AdoGen only appends to <see cref="SqlBatch.BatchCommands"/>.
/// </summary>
public static class SqlBatchExtensions
{
    extension(SqlBatch batch)
    {
        /// <summary>Appends a typed INSERT command for <typeparamref name="T"/> to the batch.</summary>
        public void Insert<T>(T model) where T : ISqlDomainModel<T>
            => T.AddInsertBatchCommand(batch, model);

        /// <summary>Appends a typed UPDATE command for <typeparamref name="T"/> to the batch.</summary>
        public void Update<T>(T model) where T : ISqlDomainModel<T>
            => T.AddUpdateBatchCommand(batch, model);

        /// <summary>Appends a typed DELETE command for <typeparamref name="T"/> to the batch.</summary>
        public void Delete<T>(T model) where T : ISqlDomainModel<T>
            => T.AddDeleteBatchCommand(batch, model);

        /// <summary>Appends a typed INSERT OR UPDATE (upsert) command for <typeparamref name="T"/> to the batch.</summary>
        public void Upsert<T>(T model) where T : ISqlDomainModel<T>
            => T.AddUpsertBatchCommand(batch, model);

        /// <summary>
        /// Appends a typed INSERT … OUTPUT INSERTED.* command for <typeparamref name="T"/> to the batch.
        /// Execute the batch with <see cref="SqlBatch.ExecuteReaderAsync(CancellationToken)"/> and call <c>T.Map(reader)</c>
        /// on the corresponding result set to read back the inserted row with server-generated values.
        /// Advance between result sets with <c>reader.NextResultAsync(ct)</c>.
        /// </summary>
        public void InsertAndReturn<T>(T model) where T : ISqlDomainModel<T>
            => T.AddInsertAndReturnBatchCommand(batch, model);
    }
}

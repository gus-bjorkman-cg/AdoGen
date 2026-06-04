using System.Threading;
using Npgsql;

namespace AdoGen.PostgreSql;

/// <summary>
/// Extension methods on <see cref="NpgsqlBatch"/> for adding AdoGen-managed commands.
/// Mix with your own <see cref="NpgsqlBatchCommand"/> instances freely — AdoGen only appends to <see cref="NpgsqlBatch.BatchCommands"/>.
/// </summary>
public static class NpgsqlBatchExtensions
{
    extension(NpgsqlBatch batch)
    {
        /// <summary>Appends a typed INSERT command for <typeparamref name="T"/> to the batch.</summary>
        public void Insert<T>(T model) where T : INpgsqlDomainModel<T>
            => T.AddInsertBatchCommand(batch, model);

        /// <summary>Appends a typed UPDATE command for <typeparamref name="T"/> to the batch.</summary>
        public void Update<T>(T model) where T : INpgsqlDomainModel<T>
            => T.AddUpdateBatchCommand(batch, model);

        /// <summary>Appends a typed DELETE command for <typeparamref name="T"/> to the batch.</summary>
        public void Delete<T>(T model) where T : INpgsqlDomainModel<T>
            => T.AddDeleteBatchCommand(batch, model);

        /// <summary>Appends a typed INSERT OR UPDATE (upsert) command for <typeparamref name="T"/> to the batch.</summary>
        public void Upsert<T>(T model) where T : INpgsqlDomainModel<T>
            => T.AddUpsertBatchCommand(batch, model);

        /// <summary>
        /// Appends a typed INSERT … RETURNING * command for <typeparamref name="T"/> to the batch.
        /// Execute the batch with <see cref="NpgsqlBatch.ExecuteReaderAsync(CancellationToken)"/> and call <c>T.Map(reader)</c>
        /// on the corresponding result set to read back the inserted row with server-generated values.
        /// Advance between result sets with <c>reader.NextResultAsync(ct)</c>.
        /// </summary>
        public void InsertAndReturn<T>(T model) where T : INpgsqlDomainModel<T>
            => T.AddInsertAndReturnBatchCommand(batch, model);
    }
}

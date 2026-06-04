using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AdoGen.SqlServer;

/// <summary>
/// Interface used by AdoGen to generate batch delete operations for composite-key models.
/// The generated code implements this interface; use the extension method on SqlConnection to call it.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ISqlCompositeKeyModel<T> where T : ISqlCompositeKeyModel<T>
{
    /// <summary>
    /// Deletes the records matching the given models' composite key columns in one roundtrip.
    /// The generated code uses a VALUES + JOIN pattern.
    /// </summary>
    static abstract ValueTask<int> DeleteAsync(
        List<T> models,
        SqlConnection connection,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null);
}

/// <summary>
/// Extension methods for ISqlCompositeKeyModel — composite-key batch delete via SqlConnection.
/// </summary>
public static class SqlConnectionCompositeKeyExtensions
{
    /// <summary>
    /// Deletes the records matching the given models' composite key columns.
    /// Uses a VALUES + JOIN pattern — efficient and supports any number of key columns.
    /// </summary>
    public static ValueTask<int> DeleteAsync<TModel>(
        this SqlConnection connection,
        List<TModel> models,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : ISqlCompositeKeyModel<TModel>
        => TModel.DeleteAsync(models, connection, ct, transaction, commandTimeout);
}


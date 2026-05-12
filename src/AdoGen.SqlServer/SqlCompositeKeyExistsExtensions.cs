using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AdoGen.SqlServer;

/// <summary>
/// Interface used by AdoGen to generate exists checks for composite-key models.
/// The generated code implements this interface; use the extension method on SqlConnection to call it.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ISqlCompositeKeyExistsModel<in T> where T : ISqlCompositeKeyExistsModel<T>
{
    /// <summary>
    /// Returns true if a record matching the given model's composite key exists in the database.
    /// The generated code uses SELECT 1 … WHERE pk1 = @pk1 AND pk2 = @pk2 via ExecuteScalarAsync.
    /// </summary>
    static abstract ValueTask<bool> ExistsAsync(
        T model,
        SqlConnection connection,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null);
}

/// <summary>
/// Extension methods for ISqlCompositeKeyExistsModel — composite-key existence check via SqlConnection.
/// </summary>
public static class SqlConnectionCompositeKeyExistsExtensions
{
    /// <summary>
    /// Returns true if a record matching the given model's composite key exists in the database.
    /// Uses SELECT 1 WHERE pk1 = @pk1 AND pk2 = @pk2 via ExecuteScalarAsync.
    /// </summary>
    public static ValueTask<bool> ExistsAsync<TModel>(
        this SqlConnection connection,
        TModel model,
        CancellationToken ct,
        SqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : ISqlCompositeKeyExistsModel<TModel>
        => TModel.ExistsAsync(model, connection, ct, transaction, commandTimeout);
}


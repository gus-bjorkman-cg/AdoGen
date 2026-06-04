using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace AdoGen.PostgreSql;

/// <summary>
/// Interface used by AdoGen to generate exists checks for composite-key models.
/// The generated code implements this interface; use the extension method on NpgsqlConnection to call it.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface INpgsqlCompositeKeyExistsModel<in T> where T : INpgsqlCompositeKeyExistsModel<T>
{
    /// <summary>
    /// Returns true if a record matching the given model's composite key exists in the database.
    /// The generated code uses SELECT 1 … WHERE pk1 = @pk1 AND pk2 = @pk2 via ExecuteScalarAsync.
    /// </summary>
    static abstract ValueTask<bool> ExistsAsync(
        T model,
        NpgsqlConnection connection,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null);
}

/// <summary>
/// Extension methods for INpgsqlCompositeKeyExistsModel — composite-key existence check via NpgsqlConnection.
/// </summary>
public static class NpgsqlConnectionCompositeKeyExistsExtensions
{
    /// <summary>
    /// Returns true if a record matching the given model's composite key exists in the database.
    /// Uses SELECT 1 WHERE pk1 = @pk1 AND pk2 = @pk2 via ExecuteScalarAsync.
    /// </summary>
    public static ValueTask<bool> ExistsAsync<TModel>(
        this NpgsqlConnection connection,
        TModel model,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : INpgsqlCompositeKeyExistsModel<TModel>
        => TModel.ExistsAsync(model, connection, ct, transaction, commandTimeout);
}

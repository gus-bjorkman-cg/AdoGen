using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace AdoGen.PostgreSql;

/// <summary>
/// Interface used by AdoGen to generate batch delete operations for composite-key models.
/// The generated code implements this interface; use the extension method on NpgsqlConnection to call it.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface INpgsqlCompositeKeyModel<T> where T : INpgsqlCompositeKeyModel<T>
{
    /// <summary>
    /// Deletes the records matching the given models' composite key columns in one roundtrip.
    /// The generated code uses a VALUES + JOIN (USING) pattern.
    /// </summary>
    static abstract ValueTask<int> DeleteAsync(
        List<T> models,
        NpgsqlConnection connection,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null);
}

/// <summary>
/// Extension methods for INpgsqlCompositeKeyModel — composite-key batch delete via NpgsqlConnection.
/// </summary>
public static class NpgsqlConnectionCompositeKeyExtensions
{
    /// <summary>
    /// Deletes the records matching the given models' composite key columns.
    /// Uses a VALUES + JOIN (USING) pattern — efficient and supports any number of key columns.
    /// </summary>
    public static ValueTask<int> DeleteAsync<TModel>(
        this NpgsqlConnection connection,
        List<TModel> models,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : INpgsqlCompositeKeyModel<TModel>
        => TModel.DeleteAsync(models, connection, ct, transaction, commandTimeout);
}


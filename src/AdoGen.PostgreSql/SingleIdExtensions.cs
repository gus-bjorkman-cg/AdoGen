using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace AdoGen.PostgreSql;

/// <summary>
/// Interface used by AdoGen to generate delete operations for models with a single key.
/// The generated code will implement this interface and the extension method below will call the generated code.
/// </summary>
public interface INpgsqlSingleIdModel<TModel, TKey>
    where TModel : INpgsqlSingleIdModel<TModel, TKey>
{
    /// <summary>
    /// Deletes the records with the given ids.
    /// The generated code will create a SQL statement with an IN clause to delete all the records in one roundtrip.
    /// </summary>
    static abstract ValueTask<int> DeleteAsync(
        NpgsqlConnection connection,
        List<TKey> ids,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null);
}

/// <summary>
/// Extensions for ISingleIdModel to call the generated delete code.
/// </summary>
public static class NpgsqlConnectionSingleIdExtensions
{
    /// <summary>
    /// Deletes the records with the given ids.
    /// The generated code will create a SQL statement with an IN clause to delete all the records in one roundtrip.
    /// </summary>
    public static async ValueTask<int> DeleteAsync<TModel, TKey>(
        this NpgsqlConnection connection,
        List<TKey> ids,
        CancellationToken ct,
        NpgsqlTransaction? transaction = null,
        int? commandTimeout = null)
        where TModel : INpgsqlSingleIdModel<TModel, TKey>
        => await TModel.DeleteAsync(connection, ids, ct, transaction, commandTimeout).ConfigureAwait(false);
}

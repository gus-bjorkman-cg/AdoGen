using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AdoGen.Abstractions;

public interface ISqlResult;

public interface ISqlResult<out T> where T : ISqlResult<T>
{
    static abstract T Map(SqlDataReader reader);
}

public interface ISqlDomainModel : ISqlResult;

public interface ISqlDomainModel<T> where T : ISqlDomainModel<T>
{
    static abstract ValueTask CreateTableAsync(SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
    static abstract ValueTask InsertAsync(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
    static abstract ValueTask InsertAsync(List<T> models, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
    static abstract ValueTask UpdateAsync(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
    static abstract ValueTask UpsertAsync(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
    static abstract ValueTask DeleteAsync(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
    static abstract ValueTask TruncateAsync(SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
}
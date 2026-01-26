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
    static abstract ValueTask CreateTable(SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
    static abstract ValueTask Insert(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
    static abstract ValueTask Insert(List<T> models, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
    static abstract ValueTask Update(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
    static abstract ValueTask Upsert(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
    static abstract ValueTask Delete(T model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
    static abstract ValueTask Truncate(SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null);
}
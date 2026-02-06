using System.Data;
using AdoGen.Abstractions;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.SqlClient;

namespace AdoGen.Benchmarks;

[BenchmarkCategory("AdoGen")]
public class AdoGenBenchmarks : TestBase
{
    [Benchmark]
    [BenchmarkCategory("FirstOrDefault")]
    public async Task FirstOrDefault()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        var param = UserSql.CreateParameterName(Index.ToString());
        Index++;
        await sqlConnection.QueryFirstOrDefaultAsync<User>(SqlGetOne, param, CancellationToken);
    }

    [Benchmark]
    [BenchmarkCategory("ToList")]
    public async Task ToList()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        var param = new SqlParameter
        {
            ParameterName = "offset",
            Value = Index,
            Direction = ParameterDirection.Input,
            DbType = DbType.Int32
        };
        await sqlConnection.QueryAsync<User>(SqlGetTen, param, CancellationToken);
        Index += 10;
    }

    [Benchmark]
    [BenchmarkCategory("Insert")]
    public async Task Insert()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        UserStream.MoveNext();
        var user = UserStream.Current;
        await sqlConnection.InsertAsync(user, CancellationToken);
    }
    
    [Benchmark]
    [BenchmarkCategory("InsertMulti")]
    public async Task InsertMulti()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        var users = UserFaker.Generate(10);
        await sqlConnection.InsertAsync(users, CancellationToken);
    }
    
    [Benchmark]
    [BenchmarkCategory("Update")]
    public async Task Update()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        var name = Index.ToString();
        Index++;
        var user = FirstUser with { Name = name };
        await sqlConnection.UpdateAsync(user, CancellationToken);
    }
}
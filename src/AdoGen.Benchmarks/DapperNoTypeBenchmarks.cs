using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.Data.SqlClient;

namespace AdoGen.Benchmarks;

[BenchmarkCategory("DapperNT")]
public class DapperNoTypeBenchmarks : TestBase
{
    [Benchmark]
    [BenchmarkCategory("QueryFirstOrDefaultAsync")]
    public async Task QueryFirstOrDefaultAsync()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        var param = new { Name = Index.ToString() };
        Index++;
        
        await sqlConnection.QueryFirstOrDefaultAsync<User>(SqlGetOne, param);
    }
    
    [Benchmark]
    [BenchmarkCategory("QueryAsync")]
    public async Task QueryAsync()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        (await sqlConnection.QueryAsync<User>(SqlGetTen)).AsList();
    }
}
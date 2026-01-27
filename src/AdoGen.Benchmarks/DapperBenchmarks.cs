using System.Data;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.Data.SqlClient;

namespace AdoGen.Benchmarks;

[BenchmarkCategory("Dapper")]
public class DapperBenchmarks : TestBase
{
    [Benchmark]
    [BenchmarkCategory("QueryFirstOrDefaultAsync")]
    public async Task QueryFirstOrDefaultAsync()
    {
        var parameters = new DynamicParameters();
        parameters.Add("Name", new DbString
        {
            IsAnsi = true,
            Length = 20,
            Value = Index.ToString()
        });
        
        Index++;
        
        var command = new CommandDefinition(
            commandText: SqlGetOne,
            parameters: parameters,
            commandType: CommandType.Text, 
            cancellationToken: CancellationToken);

        await using var sqlConnection = new SqlConnection(ConnectionString);
        await sqlConnection.QueryFirstOrDefaultAsync<User>(command);
    }
    
    [Benchmark]
    [BenchmarkCategory("QueryAsync")]
    public async Task QueryAsync()
    {
        var command = new CommandDefinition(
            commandText: SqlGetTen,
            commandType: CommandType.Text, 
            cancellationToken: CancellationToken);
        
        await using var sqlConnection = new SqlConnection(ConnectionString);
        (await sqlConnection.QueryAsync<User>(command)).AsList();
    }
}
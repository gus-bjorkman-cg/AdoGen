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
    [BenchmarkCategory("FirstOrDefault")]
    public async Task FirstOrDefault()
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
    [BenchmarkCategory("ToList")]
    public async Task ToList()
    {
        var parameters = new DynamicParameters();
        parameters.Add("offset", Index, DbType.Int32);
        var command = new CommandDefinition(
            commandText: SqlGetTen,
            commandType: CommandType.Text, 
            parameters: parameters,
            cancellationToken: CancellationToken);
        
        await using var sqlConnection = new SqlConnection(ConnectionString);
        (await sqlConnection.QueryAsync<User>(command)).AsList();
        Index += 10;
    }
}
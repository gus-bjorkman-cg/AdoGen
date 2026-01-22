using System.Data;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Dapper;
using Microsoft.Data.SqlClient;
using AdoGen.Abstractions;
using AdoGen.Sample.Features.Users;

namespace AdoGen.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class AppBenchmarks
{
    private SqlConnection _sqlConnection = null!;
    private static CancellationToken Ct => TestContext.CancellationToken;
    
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        await TestContext.InitializeAsync();
        _sqlConnection = new SqlConnection(TestContext.ConnectionString);
        await _sqlConnection.OpenAsync(Ct);
    }

    private const string Sql = "SELECT TOP(1) * FROM Users WHERE Name = @Name";
    
    [Benchmark]
    public async Task<User> Dapper()
    {
        var parameters = new DynamicParameters();
        parameters.Add("Name", new DbString
        {
            IsAnsi = true,
            Length = 20,
            Value = "3"
        });
        
        return (await _sqlConnection.QueryFirstOrDefaultAsync<User>(Sql, parameters))!;
    }
    
    [Benchmark]
    public async Task<User> AdoExtensions()
    {
        await using var command = _sqlConnection.CreateCommand(Sql);
        command.CreateParameter("@Name", "3", SqlDbType.VarChar, 20);
        
        return (await command.QueryFirstOrDefault<User>(Ct))!;
    }

    private const string SqlGetAll = "SELECT * FROM Users";
    
    [Benchmark]
    public async Task<List<User>> DapperList() => (await _sqlConnection.QueryAsync<User>(SqlGetAll)).AsList();

    [Benchmark]
    public async Task<List<User>> AdoExtensionsList() => await _sqlConnection.QueryAsync<User>(SqlGetAll, Ct);
    
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await TestContext.Dispose();
        _sqlConnection.Dispose();
    }
}
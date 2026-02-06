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
    
    private const string SqlInsert = "INSERT INTO [dbo].[Users] ([Id], [Name], [Email]) VALUES (@Id, @Name, @Email);";
    
    [Benchmark]
    [BenchmarkCategory("Insert")]
    public async Task Insert()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        UserStream.MoveNext();
        var user = UserStream.Current;
        
        var parameters = new DynamicParameters();
        parameters.Add("Id", user.Id, DbType.Guid);
        parameters.Add("Name", new DbString
        {
            IsAnsi = true,
            Length = 20,
            Value = user.Name
        });
        parameters.Add("Email", new DbString
        {
            IsAnsi = true,
            Length = 50,
            Value = user.Email
        });
        
        var command = new CommandDefinition(
            commandText: SqlInsert,
            commandType: CommandType.Text, 
            parameters: parameters,
            cancellationToken: CancellationToken);
        
        await sqlConnection.ExecuteAsync(command);
    }

    [Benchmark]
    [BenchmarkCategory("InsertMulti")]
    public async Task InsertMulti()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        var users = UserFaker.Generate(10);
        var parameters = new List<DynamicParameters>();
        foreach (var user in users)
        {
            var param = new DynamicParameters();
            param.Add("Id", user.Id, DbType.Guid);
            param.Add("Name", new DbString
            {
                IsAnsi = true,
                Length = 20,
                Value = user.Name
            });
            param.Add("Email", new DbString
            {
                IsAnsi = true,
                Length = 50,
                Value = user.Email
            });
            parameters.Add(param);
        }
        
        var command = new CommandDefinition(
            commandText: SqlInsert,
            commandType: CommandType.Text, 
            parameters: parameters,
            cancellationToken: CancellationToken);
        
        await sqlConnection.ExecuteAsync(command);
    }
}
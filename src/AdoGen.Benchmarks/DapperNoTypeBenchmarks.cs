using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.Data.SqlClient;

namespace AdoGen.Benchmarks;

[BenchmarkCategory("DapperNT")]
public class DapperNoTypeBenchmarks : TestBase
{
    [Benchmark]
    [BenchmarkCategory("FirstOrDefault")]
    public async Task FirstOrDefault()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        var param = new { Name = Index.ToString() };
        Index++;
        
        await sqlConnection.QueryFirstOrDefaultAsync<User>(SqlGetOne, param);
    }
    
    [Benchmark]
    [BenchmarkCategory("ToList")]
    public async Task ToList()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        
        (await sqlConnection.QueryAsync<User>(SqlGetTen, new { offset = Index })).AsList();
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
        await sqlConnection.ExecuteAsync(SqlInsert, user);
    }
    
    [Benchmark]
    [BenchmarkCategory("InsertMulti")]
    public async Task InsertMulti()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        var users = UserFaker.Generate(10);
        await sqlConnection.ExecuteAsync(SqlInsert, users);
    }

    private const string SqlUpdate = "UPDATE [dbo].[Users] SET Name = @Name, Email = @Email WHERE Id = @Id";
    
    [Benchmark]
    [BenchmarkCategory("Update")]
    public async Task Update()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        var name = Index.ToString();
        Index++;
        var user = FirstUser with { Name = name };
        await sqlConnection.ExecuteAsync(SqlUpdate, user);
    }
}
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Bogus;
using Dapper;
using Microsoft.Data.SqlClient;

namespace AdoGen.Benchmarks;

[BenchmarkCategory("DapperNT")]
public class DapperNoTypeBenchmarks : TestBase
{
    private static readonly Faker<User> UserFaker = new Faker<User>()
        .RuleFor(x => x.Id, Guid.CreateVersion7)
        .RuleFor(x => x.Name, y => y.Random.String2(20))
        .RuleFor(x => x.Email, y => y.Random.String2(50))
        .WithDefaultConstructor();
    
    private static readonly IEnumerator<User> UserStream = UserFaker.GenerateForever().GetEnumerator();
    
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
}
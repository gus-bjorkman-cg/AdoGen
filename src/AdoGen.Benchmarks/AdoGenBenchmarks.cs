using AdoGen.Abstractions;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Bogus;
using Microsoft.Data.SqlClient;

namespace AdoGen.Benchmarks;

[BenchmarkCategory("AdoGen")]
public class AdoGenBenchmarks : TestBase
{
    private static readonly Faker<User> UserFaker = new Faker<User>()
        .RuleFor(x => x.Id, Guid.CreateVersion7)
        .RuleFor(x => x.Name, y => y.Random.String2(20))
        .RuleFor(x => x.Email, y => y.Random.String2(50))
        .WithDefaultConstructor();
    
    private static readonly IEnumerator<User> UserStream = UserFaker.GenerateForever().GetEnumerator();

    [Benchmark]
    [BenchmarkCategory("QueryFirstOrDefaultAsync")]
    public async Task QueryFirstOrDefaultAsync()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        var param = UserSql.CreateParameterName(Index.ToString());
        Index++;
        await sqlConnection.QueryFirstOrDefaultAsync<User>(SqlGetOne, param, CancellationToken);
    }

    [Benchmark]
    [BenchmarkCategory("QueryAsync")]
    public async Task QueryAsync()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        await sqlConnection.QueryAsync<User>(SqlGetTen, CancellationToken);
    }

    [Benchmark]
    [BenchmarkCategory("AddAsync")]
    public async Task AddAsync()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        UserStream.MoveNext();
        var user = UserStream.Current;
        await sqlConnection.InsertAsync(user, CancellationToken);
    }
    
    [Benchmark]
    [BenchmarkCategory("AddRangeAsync")]
    public async Task AddRangeAsync()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        var users = UserFaker.Generate(10);
        await sqlConnection.InsertAsync(users, CancellationToken);
    }
}
public static class FakerExtensions
{
    public static Faker<T> WithDefaultConstructor<T>(this Faker<T> faker) where T : class => faker.CustomInstantiator(_ =>
    {
        var constructor = typeof(T).GetConstructors().First();
        return (T)constructor.Invoke(new object[constructor.GetParameters().Length]);
    }); 
}
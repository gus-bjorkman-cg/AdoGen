using System.Data;
using AdoGen.Abstractions;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Bogus;
using Bogus.Extensions;
using Microsoft.Data.SqlClient;

namespace AdoGen.Benchmarks;

[BenchmarkCategory("AdoGen")]
public class AdoGenBenchmarks : TestBase
{
    private static readonly Faker<User> UserFaker = new Faker<User>()
        .RuleFor(x => x.Id, Guid.CreateVersion7)
        .RuleFor(x => x.Name, y => y.Person.FullName.ClampLength(1, 20))
        .RuleFor(x => x.Email, y => y.Person.Email.ClampLength(1, 50))
        .WithDefaultConstructor();
    
    private static readonly IEnumerator<User> UserStream = UserFaker.GenerateForever().GetEnumerator();

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
}

public static class FakerExtensions
{
    public static Faker<T> WithDefaultConstructor<T>(this Faker<T> faker) where T : class =>
        faker.CustomInstantiator(_ =>
        {
            var constructor = typeof(T).GetConstructors()[0];
            return (T)constructor.Invoke(new object[constructor.GetParameters().Length]);
        });
}
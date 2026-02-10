using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;

namespace AdoGen.Benchmarks.CommandBenchmarks;

[BenchmarkCategory(nameof(BulkDelete))]
public class BulkDelete : TestBase
{
    private List<User> _users = null!;
    private List<UserModel> _userModels = null!;
    private readonly UserBulk _bulk = new();

    private const string SqlGetAll = "SELECT * FROM [dbo].[Users]";
    protected override void IterationSetup()
    {
        _bulk.Clear();
        _users = Connection.Query<User>(SqlGetAll).AsList();
        _userModels = _users.Select(x => new UserModel(x.Id, x.Name, x.Email)).ToList();
    }
    
    [Benchmark]
    public async Task AdoGen()
    {
        _bulk.RemoveRange(_users);
        await using var transaction = Connection.BeginTransaction();
        await _bulk.SaveChangesAsync(Connection, CancellationToken, transaction);
        await transaction.CommitAsync(CancellationToken);
    }

    [Benchmark]
    public async Task EfCore()
    {
        DbContext.RemoveRange(_userModels);
        await DbContext.SaveChangesAsync(CancellationToken);
    }
}
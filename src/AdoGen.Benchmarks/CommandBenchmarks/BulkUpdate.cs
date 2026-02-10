using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;

namespace AdoGen.Benchmarks.CommandBenchmarks;

[BenchmarkCategory(nameof(BulkUpdate))]
public class BulkUpdate : TestBase
{
    private List<User> _users = null!;
    private List<UserModel> _userModels = null!;
    private readonly UserBulk _bulk = new();

    private const string SqlGetAll = "SELECT * FROM [dbo].[Users]";
    protected override void IterationSetup()
    {
        _bulk.Clear();
        _users = Connection.Query<User>(SqlGetAll).Select((x, i) => x with { Name = $"other name {i}"}).AsList();
        _userModels = _users.Select(x => new UserModel(x.Id, x.Name, x.Email)).ToList();
    }
    
    [Benchmark]
    public async Task AdoGen()
    {
        _bulk.UpdateRange(_users);
        await using var transaction = Connection.BeginTransaction();
        await _bulk.SaveChangesAsync(Connection, CancellationToken, transaction);
        await transaction.CommitAsync(CancellationToken);
    }

    [Benchmark]
    public async Task EfCore()
    {
        DbContext.UpdateRange(_userModels);
        await DbContext.SaveChangesAsync(CancellationToken);
    }
}
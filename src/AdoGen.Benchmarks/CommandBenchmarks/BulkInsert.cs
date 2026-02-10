using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;

namespace AdoGen.Benchmarks.CommandBenchmarks;

[BenchmarkCategory(nameof(BulkInsert))]
public class BulkInsert : TestBase
{
    private List<User> _users = null!;
    private List<UserModel> _userModels = null!;
    private readonly UserBulk _bulk = new();
    
    protected override void IterationSetup()
    {
        _bulk.Clear();
        _users = UserFaker.Generate(1000);
        _userModels = _users.Select(x => new UserModel(x.Id, x.Name, x.Email)).ToList();
    }
    
    [Benchmark]
    public async Task AdoGen()
    {
        _bulk.AddRange(_users);
        await using var transaction = Connection.BeginTransaction();
        await _bulk.SaveChangesAsync(Connection, CancellationToken, transaction);
        await transaction.CommitAsync(CancellationToken);
    }
    
    [Benchmark]
    public async Task EfCore()
    {
        DbContext.Users.AddRange(_userModels);
        await DbContext.SaveChangesAsync(CancellationToken);
    }
}
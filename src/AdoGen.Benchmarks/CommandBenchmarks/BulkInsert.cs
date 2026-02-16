using System.Data;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AdoGen.Benchmarks.CommandBenchmarks;

[BenchmarkCategory(nameof(BulkInsert))]
public class BulkInsert : TestBase
{
    private List<User> _users = null!;
    private List<UserModel> _userModels = null!;
    private readonly UserBulk _bulk = new(1000);
    private SqlTransaction _transaction = null!;
    
    protected override async ValueTask Initialize()
    {
        _users = UserFaker.Generate(1000);
        _userModels = _users.Select(x => new UserModel(x.Id, x.Name, x.Email)).ToList();
        _transaction = Connection.BeginTransaction(IsolationLevel.ReadCommitted);
        await DbContext.Database.UseTransactionAsync(_transaction);
    }
    
    [IterationSetup]
    public void IterationSetup() => _transaction.Save("s");
    
    [IterationCleanup]
    public void IterationCleanup() => _transaction.Rollback("s");
    
    [Benchmark]
    public async Task AdoGen()
    {
        _bulk.AddRange(_users);
        await _bulk.SaveChangesAsync(Connection, _transaction, CancellationToken);
        _bulk.Clear();
    }
    
    [Benchmark]
    public async Task EfCore()
    {
        DbContext.Users.AddRange(_userModels);
        await DbContext.SaveChangesAsync(CancellationToken);
        DbContext.ChangeTracker.Clear();
    }
}
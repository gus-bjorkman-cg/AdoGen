using System.Data;
using AdoGen.PostgreSql;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AdoGen.Benchmarks.PgCommandBenchmarks;

[BenchmarkCategory(nameof(PgBulkDelete))]
public class PgBulkDelete : PgTestBase
{
    private List<User> _users = null!;
    private List<UserModel> _userModels = null!;
    private List<Guid> _ids = null!;
    private readonly UserNpgsqlBulk _bulk = new(1000);

    private const string SqlGet1K = """SELECT * FROM "public"."Users" LIMIT 1000""";

    private NpgsqlTransaction _transaction = null!;

    protected override async ValueTask Initialize()
    {
        _users = await Connection.QueryAsync<User>(SqlGet1K, CancellationToken);
        _userModels = _users.Select(x => new UserModel(x.Id, x.Name, x.Email)).ToList();
        _ids = _users.Select(x => x.Id).ToList();
        _transaction = await Connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, CancellationToken);
        await DbContext.Database.UseTransactionAsync(_transaction);
    }

    [IterationSetup]
    public void IterationSetup() => _transaction.Save("s");

    [IterationCleanup]
    public void IterationCleanup() => _transaction.Rollback("s");

    [Benchmark]
    public async Task AdoGen() => await Connection.DeleteAsync<User>(_ids, CancellationToken, _transaction);

    [Benchmark]
    public async Task AdoGenBulk()
    {
        _bulk.RemoveRange(_users);
        await _bulk.SaveChangesAsync(Connection, _transaction, CancellationToken);
        _bulk.Clear();
    }

    [Benchmark]
    public async Task EfCore()
    {
        DbContext.RemoveRange(_userModels);
        await DbContext.SaveChangesAsync(CancellationToken);
        DbContext.ChangeTracker.Clear();
    }
}


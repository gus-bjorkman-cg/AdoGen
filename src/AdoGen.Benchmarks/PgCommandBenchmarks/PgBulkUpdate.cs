using System.Data;
using AdoGen.PostgreSql;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AdoGen.Benchmarks.PgCommandBenchmarks;

[BenchmarkCategory(nameof(PgBulkUpdate))]
public class PgBulkUpdate : PgTestBase
{
    private List<User> _users = null!;
    private List<UserModel> _userModels = null!;
    private readonly UserNpgsqlBulk _bulk = new(1000);

    private const string SqlGet1K = """SELECT * FROM "public"."Users" LIMIT 1000""";

    private NpgsqlTransaction _transaction = null!;

    protected override async ValueTask Initialize()
    {
        var users = await Connection.QueryAsync<User>(SqlGet1K, CancellationToken);
        _users = users.Select((x, i) => x with { Name = $"other name {i}" }).ToList();
        _userModels = _users.Select(x => new UserModel(x.Id, x.Name, x.Email)).ToList();
        _transaction = await Connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, CancellationToken);
        await DbContext.Database.UseTransactionAsync(_transaction);
    }

    [IterationSetup]
    public void IterationSetup() => _transaction.Save("s");

    [IterationCleanup]
    public void IterationCleanup() => _transaction.Rollback("s");

    [Benchmark]
    public async Task AdoGen()
    {
        _bulk.UpdateRange(_users);
        await _bulk.SaveChangesAsync(Connection, _transaction, CancellationToken);
        _bulk.Clear();
    }

    [Benchmark]
    public async Task EfCore()
    {
        DbContext.UpdateRange(_userModels);
        await DbContext.SaveChangesAsync(CancellationToken);
        DbContext.ChangeTracker.Clear();
    }
}


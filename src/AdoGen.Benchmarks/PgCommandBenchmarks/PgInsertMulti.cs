using System.Data;
using AdoGen.PostgreSql;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AdoGen.Benchmarks.PgCommandBenchmarks;

[BenchmarkCategory(nameof(PgInsertMulti))]
public class PgInsertMulti : PgTestBase
{
    private List<User> _users = null!;
    private List<UserModel> _userModels = null!;
    private readonly UserNpgsqlBulk _bulk = new();
    private NpgsqlTransaction _transaction = null!;

    protected override async ValueTask Initialize()
    {
        _users = UserFaker.Generate(10);
        _userModels = _users.Select(x => new UserModel(x.Id, x.Name, x.Email)).ToList();
        _transaction = await Connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, CancellationToken);
        await DbContext.Database.UseTransactionAsync(_transaction);
    }

    [IterationSetup]
    public void IterationSetup() => _transaction.Save("s");

    [IterationCleanup]
    public void IterationCleanup() => _transaction.Rollback("s");

    [Benchmark]
    public async Task AdoGen() => await Connection.InsertAsync(_users, CancellationToken, _transaction);

    [Benchmark]
    public async Task AdoGenBulk()
    {
        _bulk.AddRange(_users);
        await _bulk.SaveChangesAsync(Connection, _transaction, CancellationToken);
        _bulk.Clear();
    }

    private const string SqlInsert = """INSERT INTO "public"."Users" ("Id", "Name", "Email") VALUES (@Id, @Name, @Email)""";

    [Benchmark]
    public async Task Dapper()
    {
        var parameters = new List<DynamicParameters>();
        foreach (var user in _users)
        {
            var param = new DynamicParameters();
            param.Add("Id", user.Id, DbType.Guid);
            param.Add("Name", user.Name, DbType.String, size: 20);
            param.Add("Email", user.Email, DbType.String, size: 50);
            parameters.Add(param);
        }

        var command = new CommandDefinition(
            commandText: SqlInsert,
            commandType: CommandType.Text,
            parameters: parameters,
            transaction: _transaction,
            cancellationToken: CancellationToken);

        await Connection.ExecuteAsync(command);
    }

    [Benchmark]
    public async Task DapperNoType() => await Connection.ExecuteAsync(SqlInsert, _users, _transaction);

    [Benchmark]
    public async Task EfCore()
    {
        DbContext.Users.AddRange(_userModels);
        await DbContext.SaveChangesAsync(CancellationToken);
        DbContext.ChangeTracker.Clear();
    }
}


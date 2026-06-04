using System.Data;
using AdoGen.PostgreSql;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AdoGen.Benchmarks.PgCommandBenchmarks;

[BenchmarkCategory(nameof(PgInsert))]
public class PgInsert : PgTestBase
{
    private User _user = null!;
    private UserModel _userModel = null!;
    private NpgsqlTransaction _transaction = null!;

    protected override async ValueTask Initialize()
    {
        _user = UserFaker.Generate();
        _userModel = new UserModel(_user.Id, _user.Name, _user.Email);
        _transaction = await Connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, CancellationToken);
        await DbContext.Database.UseTransactionAsync(_transaction);
    }

    [IterationSetup]
    public void IterationSetup() => _transaction.Save("s");

    [IterationCleanup]
    public void IterationCleanup() => _transaction.Rollback("s");

    [Benchmark]
    public async Task AdoGen() => await Connection.InsertAsync(_user, CancellationToken, _transaction);

    private const string SqlInsert = """INSERT INTO "public"."Users" ("Id", "Name", "Email") VALUES (@Id, @Name, @Email)""";

    [Benchmark]
    public async Task Dapper()
    {
        var parameters = new DynamicParameters();
        parameters.Add("Id", _user.Id, DbType.Guid);
        parameters.Add("Name", _user.Name, DbType.String, size: 20);
        parameters.Add("Email", _user.Email, DbType.String, size: 50);

        var command = new CommandDefinition(
            commandText: SqlInsert,
            commandType: CommandType.Text,
            parameters: parameters,
            transaction: _transaction,
            cancellationToken: CancellationToken);

        await Connection.ExecuteAsync(command);
    }

    [Benchmark]
    public async Task DapperNoType() => await Connection.ExecuteAsync(SqlInsert, _user, _transaction);

    [Benchmark]
    public async Task EfCore()
    {
        DbContext.Users.Add(_userModel);
        await DbContext.SaveChangesAsync(CancellationToken);
        DbContext.ChangeTracker.Clear();
    }
}


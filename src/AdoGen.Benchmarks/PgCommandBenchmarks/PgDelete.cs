using System.Data;
using AdoGen.PostgreSql;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AdoGen.Benchmarks.PgCommandBenchmarks;

[BenchmarkCategory(nameof(PgDelete))]
public class PgDelete : PgTestBase
{
    private User _user = null!;
    private UserModel _userModel = null!;
    private NpgsqlTransaction _transaction = null!;

    protected override async ValueTask Initialize()
    {
        _user = (await Connection.QueryFirstOrDefaultAsync<User>(SqlGetOne, UserNpgsql.CreateParameterName("250"), CancellationToken))!;
        _userModel = new UserModel(_user.Id, _user.Name, _user.Email);
        _transaction = await Connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, CancellationToken);
        await DbContext.Database.UseTransactionAsync(_transaction);
    }

    [IterationSetup]
    public void IterationSetup() => _transaction.Save("s");

    [IterationCleanup]
    public void IterationCleanup() => _transaction.Rollback("s");

    [Benchmark]
    public async Task AdoGen() => await Connection.DeleteAsync(_user, CancellationToken, _transaction);

    private const string SqlDelete = """DELETE FROM "public"."Users" WHERE "Id" = @Id""";

    [Benchmark]
    public async Task Dapper()
    {
        var parameters = new DynamicParameters();
        parameters.Add("Id", _user.Id, DbType.Guid);

        var command = new CommandDefinition(
            commandText: SqlDelete,
            commandType: CommandType.Text,
            parameters: parameters,
            transaction: _transaction,
            cancellationToken: CancellationToken);

        await Connection.ExecuteAsync(command);
    }

    [Benchmark]
    public async Task DapperNoType() => await Connection.ExecuteAsync(SqlDelete, new { _user.Id }, _transaction);

    [Benchmark]
    public async Task EfCore()
    {
        DbContext.Users.Remove(_userModel);
        await DbContext.SaveChangesAsync(CancellationToken);
        DbContext.ChangeTracker.Clear();
    }
}


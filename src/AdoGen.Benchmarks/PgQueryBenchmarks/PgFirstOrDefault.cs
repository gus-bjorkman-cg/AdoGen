using System.Data;
using System.Globalization;
using AdoGen.PostgreSql;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AdoGen.Benchmarks.PgQueryBenchmarks;

[BenchmarkCategory(nameof(PgFirstOrDefault))]
public class PgFirstOrDefault : PgTestBase
{
    private const int OperationCount = 1000;
    private string[] _names = null!;

    protected override ValueTask Initialize()
    {
        _names = Enumerable.Range(0, OperationCount).Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray();
        return ValueTask.CompletedTask;
    }

    [Benchmark(OperationsPerInvoke = OperationCount)]
    public async Task<int> AdoGen()
    {
        await using var command = new NpgsqlCommand(SqlGetOne, Connection);
        var parameter = UserNpgsql.CreateParameterName("");
        command.Parameters.Add(parameter);
        await command.PrepareAsync(CancellationToken);

        for (var i = 0; i < _names.Length; i++)
        {
            parameter.Value = _names[i];
            await command.QueryFirstOrDefaultAsync<User>(CancellationToken);
        }

        return OperationCount;
    }

    [Benchmark(OperationsPerInvoke = OperationCount)]
    public async Task<int> Dapper()
    {
        for (var i = 0; i < _names.Length; i++)
        {
            var parameters = new DynamicParameters();
            parameters.Add("Name", _names[i], DbType.String, size: 20);

            var command = new CommandDefinition(
                commandText: SqlGetOne,
                parameters: parameters,
                commandType: CommandType.Text,
                cancellationToken: CancellationToken);

            await Connection.QueryFirstOrDefaultAsync<User>(command);
        }

        return OperationCount;
    }

    [Benchmark(OperationsPerInvoke = OperationCount)]
    public async Task<int> DapperNoType()
    {
        for (var i = 0; i < _names.Length; i++)
            await Connection.QueryFirstOrDefaultAsync<User>(SqlGetOne, new { Name = _names[i] });

        return OperationCount;
    }

    [Benchmark(OperationsPerInvoke = OperationCount)]
    public async Task<int> EfCore()
    {
        for (var i = 0; i < _names.Length; i++)
        {
            var name = _names[i];
            await DbContext.Users
                .AsNoTracking()
                .Where(x => x.Name == name)
                .FirstOrDefaultAsync(CancellationToken);
        }

        return OperationCount;
    }

    private static readonly Func<PgTestDbContext, string, IAsyncEnumerable<UserModel>> CompiledByName =
        EF.CompileAsyncQuery((PgTestDbContext context, string name) =>
            context.Users.AsNoTracking().Where(x => x.Name == name));

    [Benchmark(OperationsPerInvoke = OperationCount)]
    public async Task<int> EfCoreCompiled()
    {
        for (var i = 0; i < _names.Length; i++)
            await CompiledByName(DbContext, _names[i]).FirstOrDefaultAsync(CancellationToken);

        return OperationCount;
    }
}


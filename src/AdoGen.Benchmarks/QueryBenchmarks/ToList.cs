using System.Data;
using AdoGen.Abstractions;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AdoGen.Benchmarks.QueryBenchmarks;

[BenchmarkCategory("ToList")]
public class ToList : TestBase
{
    protected override void IterationCleanup() => Index += 10;

    [Benchmark]
    public async Task AdoGen()
    {
        var param = new SqlParameter
        {
            ParameterName = "offset",
            Value = Index,
            Direction = ParameterDirection.Input,
            DbType = DbType.Int32
        };
        await Connection.QueryAsync<User>(SqlGetTen, param, CancellationToken);
    }

    [Benchmark]
    public async Task Dapper()
    {
        var parameters = new DynamicParameters();
        parameters.Add("offset", Index, DbType.Int32);
        var command = new CommandDefinition(
            commandText: SqlGetTen,
            commandType: CommandType.Text, 
            parameters: parameters,
            cancellationToken: CancellationToken);
        (await Connection.QueryAsync<User>(command)).AsList();
    }

    [Benchmark]
    public async Task DapperNoType() => 
        (await Connection.QueryAsync<User>(SqlGetTen, new { offset = Index })).AsList();
    
    [Benchmark]
    public async Task EfCore() =>
        await DbContext.Users.AsNoTracking().OrderBy(x => x.Id).Take(10).Skip(Index).ToListAsync(CancellationToken);
    
    private static readonly Func<TestDbContext, int, IAsyncEnumerable<UserModel>> CompiledUsersAll =
        EF.CompileAsyncQuery((TestDbContext context, int skip) => context.Users.AsNoTracking().OrderBy(x => x.Id).Take(10).Skip(skip));
    
    [Benchmark]
    public async Task EfCoreCompiled() =>
        await CompiledUsersAll(DbContext, Index).ToListAsync(CancellationToken);
}
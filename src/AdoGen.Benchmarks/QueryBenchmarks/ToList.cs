using System.Data;
using AdoGen.Abstractions;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AdoGen.Benchmarks.QueryBenchmarks;

[BenchmarkCategory(nameof(ToList))]
public class ToList : TestBase
{
    private const int OperationCount = 100;
    
    [Benchmark(OperationsPerInvoke = OperationCount)]
    public async Task<int> AdoGen()
    {
        await using var command = new SqlCommand(SqlGetTen, Connection);
        var parameter = new SqlParameter
        {
            ParameterName = "offset",
            Value = "",
            Direction = ParameterDirection.Input,
            DbType = DbType.Int32
        };
        command.Parameters.Add(parameter);
        command.Prepare();
        
        for (var i = 0; i < OperationCount; i+= 10)
        {
            parameter.Value = i;
            await command.QueryAsync<User>(CancellationToken);    
        }
        
        return OperationCount;
    }

    [Benchmark(OperationsPerInvoke = OperationCount)]
    public async Task<int> Dapper()
    {
        for (var i = 0; i < OperationCount; i+= 10)
        {
            var parameters = new DynamicParameters();
            parameters.Add("offset", i, DbType.Int32);
            var command = new CommandDefinition(
                commandText: SqlGetTen,
                commandType: CommandType.Text,
                parameters: parameters,
                cancellationToken: CancellationToken);
            
            (await Connection.QueryAsync<User>(command)).AsList();
        }
        
        return OperationCount;
    }

    [Benchmark(OperationsPerInvoke = OperationCount)]
    public async Task<int> DapperNoType()
    {
        for (var i = 0; i < OperationCount; i+= 10)
        {
            (await Connection.QueryAsync<User>(SqlGetTen, new { offset = i })).AsList();
        }
        
        return OperationCount;
    }

    [Benchmark(OperationsPerInvoke = OperationCount)]
    public async Task<int> EfCore()
    {
        for (var i = 0; i < OperationCount; i += 10)
        {
            await DbContext.Users.AsNoTracking().OrderBy(x => x.Id).Take(10).Skip(i).ToListAsync(CancellationToken);
        }
        
        return OperationCount;
    }

    private static readonly Func<TestDbContext, int, IAsyncEnumerable<UserModel>> CompiledUsersAll =
        EF.CompileAsyncQuery((TestDbContext context, int skip) => context.Users.AsNoTracking().OrderBy(x => x.Id).Take(10).Skip(skip));
    
    [Benchmark(OperationsPerInvoke = OperationCount)]
    public async Task<int> EfCoreCompiled()
    {
        for (var i = 0; i < OperationCount; i += 10)
        {
            await CompiledUsersAll(DbContext, i).ToListAsync(CancellationToken);
        }
        
        return OperationCount;
    }
}
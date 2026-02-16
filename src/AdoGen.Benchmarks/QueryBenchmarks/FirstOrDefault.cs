using System.Data;
using AdoGen.Abstractions;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AdoGen.Benchmarks.QueryBenchmarks;

[BenchmarkCategory(nameof(FirstOrDefault))]
public class FirstOrDefault : TestBase
{
    private const int OperationCount = 1000;
    private string[] _names = null!;

    protected override ValueTask Initialize()
    {
        _names = Enumerable.Range(0, OperationCount).Select(x => x.ToString()).ToArray();
        return ValueTask.CompletedTask;
    }

    [Benchmark(OperationsPerInvoke = OperationCount)]
    public async Task<int> AdoGen()
    {
        await using var command = new SqlCommand(SqlGetOne, Connection);
        var parameter = UserSql.CreateParameterName("");
        command.Parameters.Add(parameter);
        command.Prepare();
        
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
            var dbString = new DbString
            {
                IsAnsi = true,
                Length = 20,
                Value = _names[i]
            };
            parameters.Add("Name", dbString);

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
            await Connection.QueryFirstOrDefaultAsync<User>(SqlGetOne, new { Name = i });
        
        return OperationCount;
    }

    [Benchmark(OperationsPerInvoke = OperationCount)]
    public async Task<int> EfCore()
    {
        for (var i = 0; i < _names.Length; i++)
        {
            var name = _names[i]; // avoid closure capture
            await DbContext.Users
                .AsNoTracking()
                .Where(x => x.Name == name)
                .FirstOrDefaultAsync(CancellationToken);    
        }
        
        return OperationCount;
    }

    private static readonly Func<TestDbContext, string, IAsyncEnumerable<UserModel>> CompiledByName =
        EF.CompileAsyncQuery((TestDbContext context, string name) =>
            context.Users.AsNoTracking().Where(x => x.Name == name));
    
    [Benchmark(OperationsPerInvoke = OperationCount)]
    public async Task<int> EfCoreCompiled()
    {
        for (var i = 0; i < _names.Length; i++)
        {
            await CompiledByName(DbContext, _names[i]).FirstOrDefaultAsync(CancellationToken);
        }
        
        return OperationCount;
    }
}
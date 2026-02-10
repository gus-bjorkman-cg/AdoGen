using System.Data;
using AdoGen.Abstractions;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace AdoGen.Benchmarks.QueryBenchmarks;

[BenchmarkCategory(nameof(FirstOrDefault))]
public class FirstOrDefault : TestBase
{
    private string _name = null!;

    protected override void IterationCleanup()
    {
        Index++;
        _name = Index.ToString();
    }
    
    [Benchmark]
    public async Task AdoGen() => 
        await Connection.QueryFirstOrDefaultAsync<User>(
            SqlGetOne, 
            UserSql.CreateParameterName(_name), 
            CancellationToken);

    [Benchmark]
    public async Task Dapper()
    {
        var parameters = new DynamicParameters();
        parameters.Add("Name", new DbString
        {
            IsAnsi = true,
            Length = 20,
            Value = _name
        });
        
        var command = new CommandDefinition(
            commandText: SqlGetOne,
            parameters: parameters,
            commandType: CommandType.Text, 
            cancellationToken: CancellationToken);
        
        await Connection.QueryFirstOrDefaultAsync<User>(command);
    }

    [Benchmark]
    public async Task DapperNoType() => 
        await Connection.QueryFirstOrDefaultAsync<User>(SqlGetOne, new { Name = _name });
    
    [Benchmark]
    public async Task EfCore() =>
        await DbContext.Users
            .AsNoTracking()
            .Where(x => x.Name == _name)
            .FirstOrDefaultAsync(CancellationToken);
    
    private static readonly Func<TestDbContext, string, IAsyncEnumerable<UserModel>> CompiledByName =
        EF.CompileAsyncQuery((TestDbContext context, string name) =>
            context.Users.AsNoTracking().Where(x => x.Name == name));
    
    [Benchmark]
    public async Task EfCoreCompiled() =>
        await CompiledByName(DbContext, _name).FirstOrDefaultAsync(CancellationToken);
}
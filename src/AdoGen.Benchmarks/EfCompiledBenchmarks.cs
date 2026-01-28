using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdoGen.Benchmarks;

[BenchmarkCategory("EfCoreC")]
public class EfCompiledBenchmarks : TestBase
{
    private IDbContextFactory<TestDbContext> _factory = null!;
    
    protected override ValueTask Initialize()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<TestDbContext>(opts => opts.UseSqlServer(ConnectionString));
        var provider = services.BuildServiceProvider();
        _factory = provider.GetRequiredService<IDbContextFactory<TestDbContext>>();
        
        return ValueTask.CompletedTask;
    }
    
    private static readonly Func<TestDbContext, string, IAsyncEnumerable<UserModel>> CompiledByName =
        EF.CompileAsyncQuery((TestDbContext context, string name) =>
            context.Users.AsNoTracking().Where(u => u.Name == name));
    
    [Benchmark]
    [BenchmarkCategory("FirstOrDefault")]
    public async Task FirstOrDefault()
    {
        await using var dbContext = await _factory.CreateDbContextAsync(CancellationToken);
        var name = Index.ToString();
        Index++;
        
        await CompiledByName(dbContext, name).FirstOrDefaultAsync(CancellationToken);
    }
    
    private static readonly Func<TestDbContext, int, IAsyncEnumerable<UserModel>> CompiledUsersAll =
        EF.CompileAsyncQuery((TestDbContext context, int skip) => context.Users.AsNoTracking().OrderBy(x => x.Id).Take(10).Skip(skip));
    
    [Benchmark]
    [BenchmarkCategory("ToList")]
    public async Task ToList()
    {
        await using var dbContext = await _factory.CreateDbContextAsync(CancellationToken);
        await CompiledUsersAll(dbContext, Index).ToListAsync(CancellationToken);
        Index += 10;
    }
}
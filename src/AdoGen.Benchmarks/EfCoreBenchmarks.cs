using BenchmarkDotNet.Attributes;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace AdoGen.Benchmarks;

[BenchmarkCategory("EfCore")]
public class EfCoreBenchmarks : TestBase
{
    private static readonly Faker<UserModel> UserFaker = new Faker<UserModel>()
        .RuleFor(x => x.Id, Guid.CreateVersion7)
        .RuleFor(x => x.Name, y => y.Random.String2(20))
        .RuleFor(x => x.Email, y => y.Random.String2(50))
        .WithDefaultConstructor();
    
    private static readonly IEnumerator<UserModel> UserStream = UserFaker.GenerateForever().GetEnumerator();
    private IDbContextFactory<TestDbContext> _factory = null!;
    
    protected override ValueTask Initialize()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<TestDbContext>(opts => opts.UseSqlServer(ConnectionString));
        var provider = services.BuildServiceProvider();
        _factory = provider.GetRequiredService<IDbContextFactory<TestDbContext>>();
        
        return ValueTask.CompletedTask;
    }
    
    [Benchmark]
    [BenchmarkCategory("QueryFirstOrDefaultAsync")]
    public async Task QueryFirstOrDefaultAsync()
    {
        await using var dbContext = await _factory.CreateDbContextAsync(CancellationToken);
        var name = Index.ToString();
        Index++;

        await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Name == name)
            .FirstOrDefaultAsync(CancellationToken);
    }
    
    [Benchmark]
    [BenchmarkCategory("QueryAsync")]
    public async Task QueryAsync()
    {
        await using var dbContext = await _factory.CreateDbContextAsync(CancellationToken);
        await dbContext.Users.AsNoTracking().Take(10).ToListAsync(CancellationToken);
    }
    
    [Benchmark]
    [BenchmarkCategory("AddAsync")]
    public async Task AddAsync()
    {
        await using var dbContext = await _factory.CreateDbContextAsync(CancellationToken);
        UserStream.MoveNext();
        var user = UserStream.Current;
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(CancellationToken);
    }
    
    [Benchmark]
    [BenchmarkCategory("AddRangeAsync")]
    public async Task AddRangeAsync()
    {
        await using var dbContext = await _factory.CreateDbContextAsync(CancellationToken);
        var users = UserFaker.Generate(10);
        dbContext.Users.AddRange(users);
        await dbContext.SaveChangesAsync(CancellationToken);
    }
}

public sealed class TestDbContext : DbContext
{
    public TestDbContext() { }
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    public DbSet<UserModel> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IAssemblyMarker).Assembly);
    }
}

public sealed record UserModel(Guid Id, string Name, string Email);

public sealed class TodoItemConfiguration : IEntityTypeConfiguration<UserModel>
{
    public void Configure(EntityTypeBuilder<UserModel> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(20).IsUnicode(false);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(50).IsUnicode(false);
    }
}
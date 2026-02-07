using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdoGen.Benchmarks;

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
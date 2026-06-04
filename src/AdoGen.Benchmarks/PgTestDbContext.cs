using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdoGen.Benchmarks;

public sealed class PgTestDbContext : DbContext
{
    public PgTestDbContext() { }
    public PgTestDbContext(DbContextOptions<PgTestDbContext> options) : base(options) { }
    public DbSet<UserModel> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new PgUserConfiguration());
    }
}

public sealed class PgUserConfiguration : IEntityTypeConfiguration<UserModel>
{
    public void Configure(EntityTypeBuilder<UserModel> builder)
    {
        builder.ToTable("Users", "public");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("Id");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(20).HasColumnName("Name");
        builder.Property(x => x.Email).IsRequired().HasMaxLength(50).HasColumnName("Email");
    }
}


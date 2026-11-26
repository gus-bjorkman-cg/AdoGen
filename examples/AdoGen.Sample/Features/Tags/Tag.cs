namespace AdoGen.Sample.Features.Tags;

// DDL helpers: a non-default schema created by CreateTableAsync, a unique constraint and indexes.
public sealed partial record Tag(Guid Id, string Slug, string Name, int Category) : ISqlDomainModel, INpgsqlDomainModel;

public sealed class TagProfile : SqlProfile<Tag>
{
    public TagProfile()
    {
        Schema("catalog");
        RuleFor(x => x.Slug).VarChar(50).Unique();
        RuleFor(x => x.Name).NVarChar(50);
        HasIndex(x => new { x.Category, x.Name });
        HasIndex(x => x.Name, unique: false);
    }
}

public sealed class TagNpgsqlProfile : NpgsqlProfile<Tag>
{
    public TagNpgsqlProfile()
    {
        Schema("catalog");
        RuleFor(x => x.Slug).VarChar(50).Unique();
        RuleFor(x => x.Name).VarChar(50);
        HasIndex(x => new { x.Category, x.Name });
        HasIndex(x => x.Name, unique: false);
    }
}

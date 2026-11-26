namespace AdoGen.Generator.Tests;

/// <summary>
/// DDL helpers from docs/analysis/FEATURES.md section D: idempotent CreateTable (F29),
/// schema creation (F30) and unique constraints / indexes (F31).
/// </summary>
public sealed class DdlFeatureTests
{
    private sealed record ProviderSetup(string Usings, string Ext, string Domain, string Profile, AdoGenType DomainType);

    private static ProviderSetup Setup(string provider) =>
        provider == "SqlServer"
            ? new ProviderSetup("using System;\nusing AdoGen.SqlServer;\nnamespace Test;\n", "Sql",
                "ISqlDomainModel", "SqlProfile", AdoGenType.SqlDomainModel)
            : new ProviderSetup("using System;\nusing AdoGen.PostgreSql;\nnamespace Test;\n", "Npgsql",
                "INpgsqlDomainModel", "NpgsqlProfile", AdoGenType.NpgsqlDomainModel);

    [Theory]
    [InlineData("SqlServer",
        "IF OBJECT_ID(N'[dbo].[Tags]', N'U') IS NULL",
        ",CONSTRAINT [UQ_Tags_slug_text] UNIQUE ([slug_text])",
        "CREATE INDEX [IX_Tags_Category_slug_text] ON [dbo].[Tags] ([Category], [slug_text]);",
        "CREATE UNIQUE INDEX [UX_Tags_Name] ON [dbo].[Tags] ([Name]);")]
    [InlineData("PostgreSql",
        "CREATE TABLE IF NOT EXISTS \"public\".\"Tags\"(",
        ",CONSTRAINT \"UQ_Tags_slug_text\" UNIQUE (\"slug_text\")",
        "CREATE INDEX IF NOT EXISTS \"IX_Tags_Category_slug_text\" ON \"public\".\"Tags\" (\"Category\", \"slug_text\");",
        "CREATE UNIQUE INDEX IF NOT EXISTS \"UX_Tags_Name\" ON \"public\".\"Tags\" (\"Name\");")]
    public void CreateTable_ShouldEmitUniqueConstraintsAndIndexes_WhenProfileDeclaresThem(
        string provider, string table, string unique, string index, string uniqueIndex)
    {
        // Arrange
        var p = Setup(provider);
        var source = p.Usings + $$"""

            public sealed partial record Tag(Guid Id, string Slug, string Name, int Category) : {{p.Domain}};
            public sealed class TagProfile : {{p.Profile}}<Tag>
            {
                private const bool IsUnique = true;

                public TagProfile()
                {
                    RuleFor(x => x.Slug).Name("slug_text").VarChar(50).Unique();
                    RuleFor(x => x.Name).VarChar(50);
                    HasIndex(x => new { x.Category, x.Slug });
                    HasIndex(x => x.Name, unique: IsUnique);
                    HasIndex(x => new { x.Category, x.Slug });
                }
            }
            """;

        // Act
        var result = source.RunGenerator(p.DomainType);
        var domain = result.Result.GetGeneratedText($"Tag.Domain.{p.Ext}.g.cs");
        var errors = source.GetOutputCompilationErrors(p.DomainType);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        errors.Should().BeEmpty();
        domain.Should().Contain(table).And.Contain(unique).And.Contain(index).And.Contain(uniqueIndex);
        domain.Split(index).Length.Should().Be(2, "duplicate HasIndex calls are emitted once");
    }

    [Theory]
    [InlineData("SqlServer", "IF SCHEMA_ID(N'sales') IS NULL EXEC(N'CREATE SCHEMA [sales]');")]
    [InlineData("PostgreSql", "THEN CREATE SCHEMA \"sales\"; END IF; END $$;")]
    public void CreateTable_ShouldCreateSchema_WhenSchemaIsNotDefault(string provider, string expected)
    {
        // Arrange
        var p = Setup(provider);
        var source = p.Usings + $$"""

            public sealed partial record Invoice(Guid Id, int Number) : {{p.Domain}};
            public sealed class InvoiceProfile : {{p.Profile}}<Invoice>
            {
                public InvoiceProfile() => Schema("sales");
            }
            """;

        // Act
        var result = source.RunGenerator(p.DomainType);
        var domain = result.Result.GetGeneratedText($"Invoice.Domain.{p.Ext}.g.cs");

        // Assert
        result.Diagnostics.Should().BeEmpty();
        domain.Should().Contain(expected);
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void CreateTable_ShouldNotCreateSchema_WhenSchemaIsDefault(string provider)
    {
        // Arrange
        var p = Setup(provider);
        var source = p.Usings + $$"""

            public sealed partial record Invoice(Guid Id, int Number) : {{p.Domain}};
            public sealed class InvoiceProfile : {{p.Profile}}<Invoice>
            {
                public InvoiceProfile() { }
            }
            """;

        // Act
        var domain = source.RunGenerator(p.DomainType).Result.GetGeneratedText($"Invoice.Domain.{p.Ext}.g.cs");

        // Assert
        domain.Should().NotContain("CREATE SCHEMA");
    }

    [Theory]
    [InlineData("SqlServer", "HasIndex(x => x.Number.ToString());")]
    [InlineData("PostgreSql", "HasIndex(x => x.Number.ToString());")]
    [InlineData("SqlServer", "HasIndex(x => new { x.Number, N = x.Number });")]
    [InlineData("PostgreSql", "HasIndex(x => new { Twice = x.Number * 2 });")]
    public void AG019_ShouldBeEmitted_WhenHasIndexSelectorIsUnsupported(string provider, string call)
    {
        // Arrange
        var p = Setup(provider);
        var source = p.Usings + $$"""

            public sealed partial record Invoice(Guid Id, int Number) : {{p.Domain}};
            public sealed class InvoiceProfile : {{p.Profile}}<Invoice>
            {
                public InvoiceProfile() => {{call}}
            }
            """;

        // Act
        var result = source.RunGenerator(p.DomainType);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG019");
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void AG007_ShouldBeEmitted_WhenHasIndexUniqueIsNotConstant(string provider)
    {
        // Arrange
        var p = Setup(provider);
        var source = p.Usings + $$"""

            public sealed partial record Invoice(Guid Id, int Number) : {{p.Domain}};
            public sealed class InvoiceProfile : {{p.Profile}}<Invoice>
            {
                private static bool _unique = true;
                public InvoiceProfile() => HasIndex(x => x.Number, _unique);
            }
            """;

        // Act
        var result = source.RunGenerator(p.DomainType);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG007");
    }

    [Theory]
    [InlineData("SqlServer", "RuleFor(x => x.Number).Ignore().Unique();")]
    [InlineData("PostgreSql", "RuleFor(x => x.Number).Ignore().Unique();")]
    [InlineData("SqlServer", "RuleFor(x => x.Number).Ignore(); HasIndex(x => x.Number);")]
    [InlineData("PostgreSql", "RuleFor(x => x.Number).Ignore(); HasIndex(x => new { x.Id, x.Number });")]
    public void AG022_ShouldBeEmitted_WhenIgnoredPropertyIsIndexed(string provider, string body)
    {
        // Arrange
        var p = Setup(provider);
        var source = p.Usings + $$"""

            public sealed partial record Invoice(Guid Id, int Number) : {{p.Domain}};
            public sealed class InvoiceProfile : {{p.Profile}}<Invoice>
            {
                public InvoiceProfile() { {{body}} }
            }
            """;

        // Act
        var result = source.RunGenerator(p.DomainType);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG022");
    }

    [Theory]
    [InlineData("RuleFor(x => x.Body).NVarChar(-1).Unique();")]
    [InlineData("RuleFor(x => x.Body).VarChar(-1); HasIndex(x => new { x.Id, x.Body });")]
    public void AG025_ShouldBeEmitted_WhenSqlServerMaxColumnIsIndexed(string body)
    {
        // Arrange
        var source = $$"""
            using System;
            using AdoGen.SqlServer;
            namespace Test;

            public sealed partial record Note(Guid Id, string Body) : ISqlDomainModel;
            public sealed class NoteProfile : SqlProfile<Note>
            {
                public NoteProfile() { {{body}} }
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.SqlDomainModel);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG025");
    }

    [Fact]
    public void AG025_ShouldNotBeEmitted_WhenPostgreSqlTextColumnIsIndexed()
    {
        // Arrange
        var source = """
            using System;
            using AdoGen.PostgreSql;
            namespace Test;

            public sealed partial record Note(Guid Id, string Body) : INpgsqlDomainModel;
            public sealed class NoteProfile : NpgsqlProfile<Note>
            {
                public NoteProfile() { RuleFor(x => x.Body).Unique(); }
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.NpgsqlDomainModel);

        // Assert
        result.Diagnostics.Should().BeEmpty();
    }
}

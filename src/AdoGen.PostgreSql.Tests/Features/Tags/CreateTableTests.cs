using AdoGen.Sample.Features.Audit;
using AdoGen.Sample.Features.Tags;

namespace AdoGen.PostgreSql.Tests.Features.Tags;

public sealed class CreateTableTests(TestContext testContext) : TestBase(testContext)
{
    protected override async ValueTask DisposeAsync() => await Connection.TruncateAsync<Tag>(CancellationToken);

    [Fact]
    public async Task CreateTable_ShouldNotThrow_WhenTableAlreadyExists()
    {
        // Act
        var act = async () =>
        {
            await Connection.CreateTableAsync<Tag>(CancellationToken);
            await Connection.CreateTableAsync<AuditEvent>(CancellationToken);
        };

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateTable_ShouldCreateSchema_WhenSchemaIsMissing() =>
        (await Connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM pg_namespace WHERE nspname IN ('catalog', 'log')", CancellationToken))
            .Should().Be(2);

    [Fact]
    public async Task CreateTable_ShouldCreateIndexes_WhenProfileHasIndexes()
    {
        // Act
        var names = await Connection.QueryAsync<IndexRow>(
            """
            SELECT c.relname AS "Name", i.indisunique AS "IsUnique"
            FROM pg_index i
            JOIN pg_class c ON c.oid = i.indexrelid
            WHERE i.indrelid = '"catalog"."Tags"'::regclass
            """,
            CancellationToken);

        // Assert
        names.Should().BeEquivalentTo([
            new IndexRow("PK_Tags", true),
            new IndexRow("UQ_Tags_Slug", true),
            new IndexRow("IX_Tags_Category_Name", false),
            new IndexRow("IX_Tags_Name", false)
        ]);
    }

    [Fact]
    public async Task Insert_ShouldThrowPostgresException_WhenUniqueColumnIsDuplicated()
    {
        // Arrange
        await Connection.InsertAsync(new Tag(Guid.CreateVersion7(), "dotnet", "Dotnet", 1), CancellationToken);

        // Act
        var act = async () => await Connection.InsertAsync(new Tag(Guid.CreateVersion7(), "dotnet", "Other", 2), CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }
}

public sealed partial record IndexRow(string Name, bool IsUnique) : INpgsqlMapper;

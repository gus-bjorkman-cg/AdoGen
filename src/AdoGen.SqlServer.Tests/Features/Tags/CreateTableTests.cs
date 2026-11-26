using AdoGen.Sample.Features.Audit;
using AdoGen.Sample.Features.Tags;

namespace AdoGen.SqlServer.Tests.Features.Tags;

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
        (await Connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sys.schemas WHERE name IN ('catalog', 'log')", CancellationToken))
            .Should().Be(2);

    [Fact]
    public async Task CreateTable_ShouldCreateIndexes_WhenProfileHasIndexes()
    {
        // Act
        var names = await Connection.QueryAsync<IndexRow>(
            "SELECT name AS Name, is_unique AS IsUnique FROM sys.indexes WHERE object_id = OBJECT_ID(N'[catalog].[Tags]') AND name IS NOT NULL",
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
    public async Task Insert_ShouldThrowSqlException_WhenUniqueColumnIsDuplicated()
    {
        // Arrange
        await Connection.InsertAsync(new Tag(Guid.CreateVersion7(), "dotnet", "Dotnet", 1), CancellationToken);

        // Act
        var act = async () => await Connection.InsertAsync(new Tag(Guid.CreateVersion7(), "dotnet", "Other", 2), CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(2627);
    }
}

public sealed partial record IndexRow(string Name, bool IsUnique) : ISqlMapper;

using AdoGen.Sample.Features.Users;

namespace AdoGen.PostgreSql.Tests.Features;

public sealed class ColumnOrderTests(TestContext testContext) : TestBase(testContext)
{
    [Fact]
    public async Task Query_ShouldMapCorrectly_WhenColumnOrderDiffersBetweenQueries()
    {
        // Arrange
        var expected = DefaultUsers;

        // Act
        var declarationOrder = await Connection.QueryAsync<User>(
            """SELECT "Id", "Name", "Email" FROM "public"."Users" ORDER BY "Id" """, CancellationToken);
        var reversedOrder = await Connection.QueryAsync<User>(
            """SELECT "Email", "Name", "Id" FROM "public"."Users" ORDER BY "Id" """, CancellationToken);
        var shuffledOrder = await Connection.QueryAsync<User>(
            """SELECT "Name", "Id", "Email" FROM "public"."Users" ORDER BY "Id" """, CancellationToken);

        // Assert
        declarationOrder.Should().BeEquivalentTo(expected);
        reversedOrder.Should().Equal(declarationOrder);
        shuffledOrder.Should().Equal(declarationOrder);
    }

    [Fact]
    public async Task QueryFirstOrDefault_ShouldMapCorrectly_WhenExtraLeadingColumnsArePresent()
    {
        // Arrange
        var expected = DefaultUsers[0];

        // Act
        var actual = await Connection.QueryFirstOrDefaultAsync<User>(
            """SELECT 42 AS "Extra", 'x' AS "Other", "Email", "Id", "Name" FROM "public"."Users" WHERE "Id" = @Id""",
            new NpgsqlParameter<Guid>("Id", expected.Id),
            CancellationToken);

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task ReaderQuery_ShouldMapEachResultSet_WhenColumnOrderDiffersPerResultSet()
    {
        // Arrange
        var expected = DefaultUsers;
        await using var cmd = new NpgsqlCommand(
            """SELECT "Id", "Name", "Email" FROM "public"."Users" ORDER BY "Id"; SELECT 1 AS "Extra", "Email", "Id", "Name" FROM "public"."Users" ORDER BY "Id";""",
            Connection);

        // Act
        await using var reader = await cmd.QueryMultiAsync(CancellationToken);
        var first = await reader.QueryAsync<User>(CancellationToken);
        var second = await reader.QueryAsync<User>(CancellationToken);

        // Assert
        first.Should().BeEquivalentTo(expected);
        second.Should().Equal(first);
    }
}

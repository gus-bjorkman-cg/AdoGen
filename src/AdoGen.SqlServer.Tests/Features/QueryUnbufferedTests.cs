using System.Data;
using AdoGen.Sample.Features.Users;

namespace AdoGen.SqlServer.Tests.Features;

public sealed class QueryUnbufferedTests(TestContext testContext) : TestBase(testContext)
{
    private const string Sql = """SELECT * FROM Users""";
    private const string EmptySql = """SELECT * FROM Users WHERE 1 = 0""";
    private const string ByIdSql = """SELECT * FROM Users WHERE "Id" = @Id""";
    private const string CountSql = """SELECT COUNT(*) FROM Users""";

    [Fact]
    public async Task QueryUnbuffered_ShouldStreamAllRows()
    {
        // Act
        var actual = new List<User>();
        await foreach (var user in Connection.QueryUnbufferedAsync<User>(Sql, CancellationToken)) actual.Add(user);

        // Assert
        actual.Should().BeEquivalentTo(DefaultUsers);
    }

    [Fact]
    public async Task QueryUnbuffered_ShouldYieldNothing_WhenNoRows()
    {
        // Act
        var count = 0;
        await foreach (var _ in Connection.QueryUnbufferedAsync<User>(EmptySql, CancellationToken)) count++;

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task QueryUnbuffered_ShouldBindSpanParameters()
    {
        // Act
        var actual = new List<User>();
        await foreach (var user in Connection.QueryUnbufferedAsync<User>(ByIdSql, [UserSql.CreateParameterId(DefaultUsers[0].Id)], CancellationToken))
            actual.Add(user);

        // Assert
        actual.Should().ContainSingle().Which.Should().BeEquivalentTo(DefaultUsers[0]);
    }

    [Fact]
    public async Task QueryUnbuffered_ShouldLeaveConnectionUsable_WhenEnumerationStopsEarly()
    {
        // Arrange
        await Connection.InsertAsync(UserFaker.Generate(500), CancellationToken);

        // Act
        await foreach (var _ in Connection.QueryUnbufferedAsync<User>(Sql, CancellationToken)) break;

        // Assert
        Connection.State.Should().Be(ConnectionState.Open);
        (await Connection.ExecuteScalarAsync<int>(CountSql, CancellationToken)).Should().Be(DefaultUsers.Count + 500);
    }

    [Fact]
    public async Task QueryUnbuffered_ShouldCloseConnection_WhenItOpenedIt()
    {
        // Arrange
        await using var connection = new SqlConnection(ConnectionString);

        // Act
        await foreach (var _ in connection.QueryUnbufferedAsync<User>(Sql, CancellationToken)) break;

        // Assert
        connection.State.Should().Be(ConnectionState.Closed);
    }
}

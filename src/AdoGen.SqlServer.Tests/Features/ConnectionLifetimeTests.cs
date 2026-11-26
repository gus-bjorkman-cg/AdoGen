using System.Data;
using AdoGen.Sample.Features.Users;

namespace AdoGen.SqlServer.Tests.Features;

public sealed class ConnectionLifetimeTests(TestContext testContext) : TestBase(testContext)
{
    private const string Sql = """SELECT * FROM Users""";

    [Fact]
    public async Task Query_ShouldCloseConnection_WhenConnectionWasClosed()
    {
        // Arrange
        await using var connection = new SqlConnection(ConnectionString);

        // Act
        var users = await connection.QueryAsync<User>(Sql, CancellationToken);

        // Assert
        users.Should().HaveCount(DefaultUsers.Count);
        connection.State.Should().Be(ConnectionState.Closed);
    }

    [Fact]
    public async Task Insert_ShouldCloseConnection_WhenConnectionWasClosed()
    {
        // Arrange
        await using var connection = new SqlConnection(ConnectionString);

        // Act
        await connection.InsertAsync(UserFaker.Generate(), CancellationToken);

        // Assert
        connection.State.Should().Be(ConnectionState.Closed);
    }

    [Fact]
    public async Task Execute_ShouldLeaveConnectionOpen_WhenConnectionWasOpen()
    {
        // Act
        await Connection.ExecuteAsync("SELECT 1", CancellationToken);

        // Assert
        Connection.State.Should().Be(ConnectionState.Open);
    }

    [Fact]
    public async Task QueryMulti_ShouldCloseConnectionWithReader_WhenConnectionWasClosed()
    {
        // Arrange
        await using var connection = new SqlConnection(ConnectionString);

        // Act
        var reader = await connection.QueryMultiAsync(Sql, CancellationToken);
        var openWhileReading = connection.State;
        await reader.DisposeAsync();

        // Assert
        openWhileReading.Should().Be(ConnectionState.Open);
        connection.State.Should().Be(ConnectionState.Closed);
    }
}

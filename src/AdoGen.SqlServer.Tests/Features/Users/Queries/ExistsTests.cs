using AdoGen.Sample.Features.Users;

namespace AdoGen.SqlServer.Tests.Features.Users.Queries;

public sealed class ExistsTests(TestContext testContext) : TestBase(testContext)
{
    [Fact]
    public async Task Exists_ShouldReturnTrue_WhenUserExists() => 
        (await Connection.ExistsAsync<User, Guid>(DefaultUsers[0].Id, CancellationToken)).Should().BeTrue();

    [Fact]
    public async Task Exists_ShouldReturnFalse_WhenUserDoesNotExist() => 
        (await Connection.ExistsAsync<User, Guid>(Guid.NewGuid(), CancellationToken)).Should().BeFalse();

    [Fact]
    public async Task Exists_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = async () => await Connection.ExistsAsync<User, Guid>(DefaultUsers[0].Id, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Exists_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");

        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await connectionB.ExistsAsync<User, Guid>(DefaultUsers[0].Id, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(-2);
        transaction.Rollback();
    }

    [Fact]
    public async Task Exists_ShouldRespectDbTransaction()
    {
        // Arrange
        var user = UserFaker.Generate();
        await using var transaction = Connection.BeginTransaction();
        await Connection.InsertAsync(user, CancellationToken, transaction);

        // Act
        var withinTx = await Connection.ExistsAsync<User, Guid>(user.Id, CancellationToken, transaction);

        // Assert
        withinTx.Should().BeTrue();
    }
}

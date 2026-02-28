namespace AdoGen.PostgreSql.Tests.Features;

public sealed class UpsertTests(TestContext testContext) : TestBase(testContext)
{
    [Fact]
    public async Task User_ShouldBeUpdated_WhenExisting()
    {
        // Arrange
        var user = DefaultUsers[0] with { Name = "other name" };

        // Act
        await Connection.UpsertAsync(user, CancellationToken);

        // Assert
        (await GetUser(user.Id)).Should().Be(user);
    }

    [Fact]
    public async Task User_ShouldBeCreated_WhenNotExisting()
    {
        // Arrange
        var user = UserFaker.Generate();

        // Act
        await Connection.UpsertAsync(user, CancellationToken);

        // Assert
        (await GetUser(user.Id)).Should().Be(user);
    }

    [Fact]
    public async Task Upsert_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);
        var user = DefaultUsers[0] with { Name = "other name" };

        // Act
        await Connection.UpsertAsync(user, CancellationToken, transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        (await GetUser(user.Id)).Should().Be(DefaultUsers[0]);
    }
}


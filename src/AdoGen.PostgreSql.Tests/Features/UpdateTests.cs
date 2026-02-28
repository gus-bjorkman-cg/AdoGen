namespace AdoGen.PostgreSql.Tests.Features;

public sealed class UpdateTests(TestContext testContext) : TestBase(testContext)
{
    [Fact]
    public async Task User_ShouldBeUpdated()
    {
        // Arrange
        var user = DefaultUsers[0] with { Name = "other name" };

        // Act
        await Connection.UpdateAsync(user, CancellationToken);

        // Assert
        (await GetUser(user.Id)).Should().Be(user);
    }

    [Fact]
    public async Task Update_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);
        var user = DefaultUsers[0] with { Name = "other name" };

        // Act
        await Connection.UpdateAsync(user, CancellationToken, transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        (await GetUser(user.Id)).Should().Be(DefaultUsers[0]);
    }
}


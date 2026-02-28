namespace AdoGen.PostgreSql.Tests.Features;

public sealed class DeleteTests(TestContext testContext) : TestBase(testContext)
{
    [Fact]
    public async Task User_ShouldBeDeleted()
    {
        // Act
        await Connection.DeleteAsync(DefaultUsers[0], CancellationToken);

        // Assert
        (await GetUser(DefaultUsers[0].Id)).Should().BeNull();
    }

    [Fact]
    public async Task Delete_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);

        // Act
        await Connection.DeleteAsync(DefaultUsers[0], CancellationToken, transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        (await GetUser(DefaultUsers[0].Id)).Should().Be(DefaultUsers[0]);
    }
}


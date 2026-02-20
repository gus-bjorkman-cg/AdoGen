namespace AdoGen.Tests.Features;

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
        await using var transaction = Connection.BeginTransaction();
        var user = DefaultUsers[0] with { Name = "other name" };
        
        // Act
        await Connection.UpdateAsync(user, CancellationToken, transaction);
        transaction.Rollback();

        // Assert
        (await GetUser(user.Id)).Should().Be(DefaultUsers[0]);
    }

    [Fact]
    public async Task Update_ShouldRespectCommandTimeout()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        
        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await Connection.UpdateAsync(DefaultUsers[0], CancellationToken, commandTimeout: 1);
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        transaction.Rollback();
    }
}
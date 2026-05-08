namespace AdoGen.SqlServer.Tests.Features;

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
    public async Task Update_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var user = DefaultUsers[0] with { Name = "other name" };
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        
        // Act
        var act = async () => await Connection.UpdateAsync(user, cts.Token);
        
        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
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
    public async Task Update_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        
        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await connectionB.UpdateAsync(DefaultUsers[0], CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(-2);
        transaction.Rollback();
    }
}
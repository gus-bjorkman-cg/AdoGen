namespace AdoGen.SqlServer.Tests.Features;

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
    public async Task Delete_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        
        // Act
        var act = async () => await Connection.DeleteAsync(DefaultUsers[0], cts.Token);
        
        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
    
    [Fact]
    public async Task Delete_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = Connection.BeginTransaction();
        
        // Act
        await Connection.DeleteAsync(DefaultUsers[0], CancellationToken, transaction);
        transaction.Rollback();

        // Assert
        (await GetUser(DefaultUsers[0].Id)).Should().Be(DefaultUsers[0]);
    }

    [Fact]
    public async Task Delete_ShouldRespectCommandTimeout()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        
        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await connectionB.DeleteAsync(DefaultUsers[0], CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(-2);
        transaction.Rollback();
    }
}
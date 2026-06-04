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
    public async Task Upsert_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        
        // Act
        var act = async () => await Connection.UpsertAsync(UserFaker.Generate(), cts.Token);
        
        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
    
    [Fact]
    public async Task Upsert_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        
        // Act
        var act = async () =>
        {
            await using var connectionB = new NpgsqlConnection(ConnectionString);
            await connectionB.UpsertAsync(UserFaker.Generate(), CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<NpgsqlException>()).WithInnerException<TimeoutException>();
        await transaction.RollbackAsync(CancellationToken);
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


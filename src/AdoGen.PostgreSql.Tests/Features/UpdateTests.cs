using AdoGen.Sample.Features.Users;

namespace AdoGen.PostgreSql.Tests.Features;

public sealed class UpdateTests : TestBase
{
    private readonly User _user;
    
    public UpdateTests(TestContext testContext) : base(testContext)
    {
        _user = DefaultUsers[0] with { Name = "other name" };
    }

    [Fact]
    public async Task Update_ShouldUpdateEntity()
    {
        // Act
        await Connection.UpdateAsync(_user, CancellationToken);

        // Assert
        (await GetUser(_user.Id)).Should().Be(_user);
    }
    
    [Fact]
    public async Task Update_ShouldReturnZero_WhenIdNotFound() => 
        (await Connection.UpdateAsync(DefaultUsers[0] with { Id = Guid.CreateVersion7() }, CancellationToken))
        .Should().Be(0);
    
    [Fact]
    public async Task Update_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        
        // Act
        var act = async () => await Connection.UpdateAsync(_user, cts.Token);
        
        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
    
    [Fact]
    public async Task Update_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        
        // Act
        var act = async () =>
        {
            await using var connectionB = new NpgsqlConnection(ConnectionString);
            await connectionB.UpdateAsync(_user, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<NpgsqlException>()).WithInnerException<TimeoutException>();
        await transaction.RollbackAsync(CancellationToken);
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


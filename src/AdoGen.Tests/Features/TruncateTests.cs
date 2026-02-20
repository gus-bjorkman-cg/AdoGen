using AdoGen.Sample.Features.Users;

namespace AdoGen.Tests.Features;

public sealed class TruncateTests(TestContext testContext) : TestBase(testContext)
{
    [Fact]
    public async Task UsersCount_ShouldBeZero_WhenTruncated()
    {
        // Act
        await Connection.TruncateAsync<User>(CancellationToken);
        
        // Assert
        (await GetUsersCount()).Should().Be(0);
    }
    
    [Fact]
    public async Task Truncate_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = Connection.BeginTransaction();
        
        // Act
        await Connection.TruncateAsync<User>(CancellationToken, transaction);
        transaction.Rollback();

        // Assert
        (await GetUsersCount()).Should().BeGreaterThan(0);
    }
    
    [Fact]
    public async Task Truncate_ShouldRespectCommandTimeout()
    {
        // Arrange
        await using var transaction = await LockUserTable();
        
        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await connectionB.TruncateAsync<User>(CancellationToken, commandTimeout: 1);
        };

        // Assert
        await act.Should().ThrowAsync<SqlException>();
        transaction.Rollback();
    }

    private async ValueTask<int> GetUsersCount()
    {
        await using var command = Connection.CreateCommand("SELECT COUNT(*) FROM Users");
        return (int)await command.ExecuteScalarAsync(CancellationToken);
    }
}
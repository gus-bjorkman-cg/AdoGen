using System.Globalization;
using AdoGen.Sample.Features.Users;

namespace AdoGen.PostgreSql.Tests.Features;

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
    public async Task Truncate_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        
        // Act
        var act = async () => await Connection.TruncateAsync<User>(cts.Token);
        
        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
    
    [Fact]
    public async Task Truncate_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        
        // Act
        var act = async () =>
        {
            await using var connectionB = new NpgsqlConnection(ConnectionString);
            await connectionB.TruncateAsync<User>(CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<NpgsqlException>()).WithInnerException<TimeoutException>();
        await transaction.RollbackAsync(CancellationToken);
    }
    
    [Fact]
    public async Task Truncate_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);

        // Act
        await Connection.TruncateAsync<User>(CancellationToken, transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        (await GetUsersCount()).Should().BeGreaterThan(0);
    }

    private async ValueTask<long> GetUsersCount()
    {
        await using var command = Connection.CreateCommand("""SELECT COUNT(*) FROM "public"."Users" """);
        var value = await command.ExecuteScalarAsync(CancellationToken);
        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }
}


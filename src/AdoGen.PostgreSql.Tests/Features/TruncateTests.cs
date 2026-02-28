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
        return Convert.ToInt64(value);
    }
}


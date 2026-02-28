using AdoGen.Sample.Features.Users;

namespace AdoGen.PostgreSql.Tests.Features;

public sealed class InsertTests(TestContext testContext) : TestBase(testContext)
{
    private readonly User _user = UserFaker.Generate();

    [Fact]
    public async Task User_ShouldBeInserted()
    {
        // Act
        await Connection.InsertAsync(_user, CancellationToken);

        // Assert
        (await GetUser(_user.Id)).Should().BeEquivalentTo(_user);
    }

    [Fact]
    public async Task Insert_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);

        // Act
        await Connection.InsertAsync(_user, CancellationToken, transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        (await GetUser(_user.Id)).Should().BeNull();
    }
}


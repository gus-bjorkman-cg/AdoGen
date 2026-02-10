using AdoGen.Abstractions;
using Microsoft.Data.SqlClient;

namespace AdoGen.Tests.Features;

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
        await using var transaction = await LockUserTable();
        
        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await Connection.DeleteAsync(DefaultUsers[0], CancellationToken, commandTimeout: 1);
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        transaction.Rollback();
    }
}
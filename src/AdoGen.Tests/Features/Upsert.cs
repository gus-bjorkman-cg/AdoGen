using AdoGen.Abstractions;
using Microsoft.Data.SqlClient;

namespace AdoGen.Tests.Features;

public sealed class Upsert(TestContext testContext) : TestBase(testContext)
{
    [Fact]
    public async Task User_ShouldBeUpdated_WhenExisting()
    {
        // Arrange
        var user = DefaultUsers[0] with { Name = "other name" };
        
        // Act
        await Connection.UpsertAsync(user, Ct);
        
        // Assert
        (await GetUser(user.Id)).Should().Be(user);
    }
    
    [Fact]
    public async Task User_ShouldBeCreated_WhenNotExisting()
    {
        // Arrange
        var user = UserFaker.Generate();
        
        // Act
        await Connection.UpsertAsync(user, Ct);
        
        // Assert
        (await GetUser(user.Id)).Should().Be(user);
    }

    [Fact]
    public async Task Upsert_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = Connection.BeginTransaction();
        var user = DefaultUsers[0] with { Name = "other name" };
        
        // Act
        await Connection.UpsertAsync(user, Ct, transaction);
        transaction.Rollback();

        // Assert
        (await GetUser(user.Id)).Should().Be(DefaultUsers[0]);
    }

    [Fact]
    public async Task Upsert_ShouldRespectCommandTimeout()
    {
        // Arrange
        await using var transaction = await LockUserTable();
        
        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await Connection.UpsertAsync(DefaultUsers[0], Ct, commandTimeout: 1);
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        transaction.Rollback();
    }
}
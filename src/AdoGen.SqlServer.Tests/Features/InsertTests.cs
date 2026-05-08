using AdoGen.Sample.Features.Users;

namespace AdoGen.SqlServer.Tests.Features;

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
    public async Task Insert_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        
        // Act
        var act = async () => await Connection.InsertAsync(_user, cts.Token);
        
        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Insert_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = Connection.BeginTransaction();
        
        // Act
        await Connection.InsertAsync(_user, CancellationToken, transaction);
        transaction.Rollback();

        // Assert
        (await GetUser(_user.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Insert_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        
        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await connectionB.InsertAsync(_user, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(-2);
        transaction.Rollback();
    }
}
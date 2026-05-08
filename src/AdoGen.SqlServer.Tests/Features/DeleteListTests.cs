using AdoGen.Sample.Features.Users;

namespace AdoGen.SqlServer.Tests.Features;

public class DeleteListTests : TestBase
{
    private readonly List<Guid> _ids;
    
    public DeleteListTests(TestContext testContext) : base(testContext) => 
        _ids = DefaultUsers.Select(x => x.Id).ToList();

    [Fact]
    public async Task User_ShouldBeDeleted()
    {
        // Act
        await Connection.DeleteAsync<User, Guid>(_ids, CancellationToken);

        // Assert
        (await GetAllUsers()).Should().BeEmpty();
    }
    
    [Fact]
    public async Task Execute_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        
        // Act
        var act = async () => await Connection.DeleteAsync<User, Guid>(_ids, cts.Token);
        
        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
    
    [Fact]
    public async Task Execute_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        
        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await connectionB.DeleteAsync<User, Guid>(_ids, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(-2);
        transaction.Rollback();
    }
    
    [Fact]
    public async Task Execute_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = Connection.BeginTransaction();
        
        // Act
        await Connection.DeleteAsync<User, Guid>(_ids, CancellationToken, transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        (await GetAllUsers()).Should().BeEquivalentTo(DefaultUsers);
    }
}
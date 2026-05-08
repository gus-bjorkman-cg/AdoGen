using AdoGen.Sample.Features.Users;

namespace AdoGen.PostgreSql.Tests.Features;

public class DeleteListTests : TestBase
{
    private readonly List<Guid> _ids;
    
    public DeleteListTests(TestContext testContext) : base(testContext) => 
        _ids = DefaultUsers.Select(x => x.Id).ToList();
    
    [Fact]
    public async Task User_ShouldBeDeleted()
    {
        // Arrange
        var ids = DefaultUsers.Select(x => x.Id).ToList();

        // Act
        await Connection.DeleteAsync<User, Guid>(ids, CancellationToken);

        // Assert
        var users = await Connection.QueryAsync<User>("""SELECT * FROM "public"."Users" """, CancellationToken);
        users.Should().BeEmpty();
    }
    
    [Fact]
    public async Task Delete_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
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
    public async Task Delete_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        
        // Act
        var act = async () =>
        {
            await using var connectionB = new NpgsqlConnection(ConnectionString);
            await connectionB.DeleteAsync<User, Guid>(_ids, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<NpgsqlException>()).WithInnerException<TimeoutException>();
        await transaction.RollbackAsync(CancellationToken);
    }
    
    [Fact]
    public async Task Delete_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);
        
        // Act
        await Connection.DeleteAsync<User, Guid>(_ids, CancellationToken, transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        (await GetAllUsers()).Should().BeEquivalentTo(DefaultUsers);
    }
}


using AdoGen.Sample.Features.Users;

namespace AdoGen.SqlServer.Tests.Features;

public sealed class ExecuteTests(TestContext testContext) : TestBase(testContext)
{
    private const string SqlUpdate = "UPDATE Users SET Name = 'updated'";
    
    [Fact]
    public async Task Execute_ShouldReturnAffectedRows()
    {
        // Act
        var actual = await Connection.ExecuteAsync(SqlUpdate, CancellationToken);

        // Assert
        actual.Should().Be((await GetAllUsers()).Count);
    }

    [Fact]
    public async Task Execute_ShouldAffectCorrectRows()
    {
        // Act
        await Connection.ExecuteAsync(SqlUpdate, CancellationToken);

        // Assert
        (await GetAllUsers()).Select(x => x.Name).Should().AllSatisfy(x => x.Should().Be("updated"));
    }

    [Fact]
    public async Task Execute_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        
        // Act
        var act = async () => await Connection.ExecuteAsync(SqlUpdate, cts.Token);
        
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
            await connectionB.ExecuteAsync(SqlUpdate, CancellationToken, commandTimeout: 1);
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
        await Connection.ExecuteAsync(SqlUpdate, CancellationToken, transaction: transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        (await GetUser(DefaultUsers[0].Id)).Should().Be(DefaultUsers[0]);
    }
    
    private const string SqlUpdateSingleParam = "UPDATE Users SET Name = 'updated' WHERE Id = @Id";
    
    [Fact]
    public async Task Execute_ShouldReturnAffectedRows_WhenSingleParameter()
    {
        // Act
        var actual = await Connection.ExecuteAsync(SqlUpdateSingleParam, UserSql.CreateParameterId(DefaultUsers[0].Id),
            CancellationToken);

        // Assert
        actual.Should().Be(1);
    }

    [Fact]
    public async Task Execute_ShouldAffectCorrectRow_WhenSingleParameter()
    {
        // Arrange
        var id = DefaultUsers[0].Id;
        
        // Act
        await Connection.ExecuteAsync(SqlUpdateSingleParam, UserSql.CreateParameterId(id), CancellationToken);

        // Assert
        (await GetUser(id))!.Name.Should().Be("updated");
    }
    
    [Fact]
    public async Task Execute_ShouldNotAffectOtherRows_WhenSingleParameter()
    {
        // Arrange
        var id = DefaultUsers[0].Id;
        
        // Act
        await Connection.ExecuteAsync(SqlUpdateSingleParam, UserSql.CreateParameterId(id), CancellationToken);

        // Assert
        (await GetAllUsers()).Where(x => x.Id != id).Select(x => x.Name).Should()
            .AllSatisfy(x => x.Should().NotBe("updated"));
    }

    private const string SqlUpdateMultiParam = "UPDATE Users SET Name = @Name WHERE Id = @Id";
        
    [Fact]
    public async Task Execute_ShouldReturnAffectedRows_WhenMultipleParameters()
    {
        // Arrange
        var id = DefaultUsers[0].Id;

        // Act
        var affected = await Connection.ExecuteAsync(
            SqlUpdateMultiParam, 
            [UserSql.CreateParameterId(id), UserSql.CreateParameterName("multi param")], 
            CancellationToken);

        // Assert
        affected.Should().Be(1);
    }
    
    [Fact]
    public async Task Execute_ShouldAffectCorrectRow_WhenMultipleParameters()
    {
        // Arrange
        var id = DefaultUsers[0].Id;

        // Act
        await Connection.ExecuteAsync(
            SqlUpdateMultiParam, 
            [UserSql.CreateParameterId(id), UserSql.CreateParameterName("multi param")], 
            CancellationToken);

        // Assert
        (await GetUser(id))!.Name.Should().Be("multi param");
    }
    
    [Fact]
    public async Task Execute_ShouldNotAffectOtherRows_WhenMultipleParameters()
    {
        // Arrange
        var id = DefaultUsers[0].Id;
        
        // Act
        await Connection.ExecuteAsync(
            SqlUpdateMultiParam, 
            [UserSql.CreateParameterId(id), UserSql.CreateParameterName("multi param")], 
            CancellationToken);

        // Assert
        (await GetAllUsers()).Where(x => x.Id != id).Select(x => x.Name).Should()
            .AllSatisfy(x => x.Should().NotBe("multi param"));
    }
}
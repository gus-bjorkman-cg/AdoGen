using AdoGen.Sample.Features.Users;

namespace AdoGen.PostgreSql.Tests.Features;

public sealed class ExecuteTests(TestContext testContext) : TestBase(testContext)
{
    private const string SqlUpdate = """UPDATE "public"."Users" SET "Name" = 'updated'""";
    
    [Fact]
    public async Task Execute_ShouldReturnAffectedRows()
    {
        // Act
        var actual = await Connection.ExecuteAsync(SqlUpdate, CancellationToken);

        // Assert
        var count = (await GetAllUsers()).Count;
        actual.Should().Be(count);
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
    public async Task Execute_ShouldThrowTimeoutException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        
        // Act
        var act = async () =>
        {
            await using var connectionB = new NpgsqlConnection(ConnectionString);
            await connectionB.ExecuteAsync(SqlUpdate, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<NpgsqlException>()).WithInnerException<TimeoutException>();
        transaction.Rollback();
    }
    
    [Fact]
    public async Task Execute_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);
        
        // Act
        await Connection.ExecuteAsync(SqlUpdate, CancellationToken, transaction: transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        (await GetUser(DefaultUsers[0].Id)).Should().Be(DefaultUsers[0]);
    }
    
    private const string SqlUpdateSingleParam = """UPDATE "public"."Users" SET "Name" = 'updated' WHERE "Id" = @Id""";
    
    [Fact]
    public async Task Execute_ShouldReturnAffectedRows_WhenSingleParameter()
    {
        // Act
        var actual = await Connection.ExecuteAsync(SqlUpdateSingleParam, UserNpgsql.CreateParameterId(DefaultUsers[0].Id),
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
        await Connection.ExecuteAsync(SqlUpdateSingleParam, UserNpgsql.CreateParameterId(id), CancellationToken);

        // Assert
        (await GetUser(id))!.Name.Should().Be("updated");
    }
    
    [Fact]
    public async Task Execute_ShouldNotAffectOtherRows_WhenSingleParameter()
    {
        // Arrange
        var id = DefaultUsers[0].Id;
        
        // Act
        await Connection.ExecuteAsync(SqlUpdateSingleParam, UserNpgsql.CreateParameterId(id), CancellationToken);

        // Assert
        (await GetAllUsers()).Where(x => x.Id != id).Select(x => x.Name).Should()
            .AllSatisfy(x => x.Should().NotBe("updated"));
    }

    private const string SqlUpdateMultiParam = """UPDATE "public"."Users" SET "Name" = @Name WHERE "Id" = @Id""";
    
    [Fact]
    public async Task Execute_ShouldReturnAffectedRows_WhenMultipleParameters()
    {
        // Arrange
        var id = DefaultUsers[0].Id;

        // Act
        var affected = await Connection.ExecuteAsync(
            SqlUpdateMultiParam, 
            [UserNpgsql.CreateParameterId(id), UserNpgsql.CreateParameterName("multi param")], 
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
            [UserNpgsql.CreateParameterId(id), UserNpgsql.CreateParameterName("multi param")], 
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
            [UserNpgsql.CreateParameterId(id), UserNpgsql.CreateParameterName("multi param")], 
            CancellationToken);

        // Assert
        (await GetAllUsers()).Where(x => x.Id != id).Select(x => x.Name).Should()
            .AllSatisfy(x => x.Should().NotBe("multi param"));
    }
}
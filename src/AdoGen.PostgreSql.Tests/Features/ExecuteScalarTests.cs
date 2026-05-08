using NpgsqlTypes;

namespace AdoGen.PostgreSql.Tests.Features;

public sealed class ExecuteScalarTests(TestContext testContext) : TestBase(testContext)
{
    private const string Sql = """SELECT COUNT(*) FROM "public"."Users" """;
    
    [Fact]
    public async Task ExecuteScalar_ShouldReturnValue()
    {
        // Act
        var actual = await Connection.ExecuteScalarAsync<long>(Sql, CancellationToken);

        // Assert
        actual.Should().Be((await GetAllUsers()).Count);
    }
    
    [Fact]
    public async Task ExecuteScalar_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        
        // Act
        var act = async () => await Connection.ExecuteScalarAsync<long>(Sql, cts.Token);
        
        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
    
    [Fact]
    public async Task ExecuteScalar_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        
        // Act
        var act = async () =>
        {
            await using var connectionB = new NpgsqlConnection(ConnectionString);
            await connectionB.ExecuteScalarAsync<int>(Sql, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<NpgsqlException>()).WithInnerException<TimeoutException>();
        await transaction.RollbackAsync(CancellationToken);
    }
    
    [Fact]
    public async Task ExecuteScalar_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);
        await Connection.ExecuteAsync("""TRUNCATE TABLE "public"."Users" """, CancellationToken, transaction: transaction);
        
        // Act
        var actual = await Connection.ExecuteScalarAsync<long>(Sql, CancellationToken, transaction: transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        actual.Should().Be(0);
    }
    
    private const string SqlWithParam = """SELECT COUNT(*) FROM "public"."Users" WHERE 1 = @p1""";

    private readonly NpgsqlParameter _parameter1 = new()
        { ParameterName = "p1", Value = 1, NpgsqlDbType = NpgsqlDbType.Integer };
    
    [Fact]
    public async Task ExecuteScalarSingleParameter_ShouldReturnValue()
    {
        // Act
        var actual = await Connection.ExecuteScalarAsync<long>(SqlWithParam, _parameter1, CancellationToken);

        // Assert
        actual.Should().Be((await GetAllUsers()).Count);
    }
    
    [Fact]
    public async Task ExecuteScalarSingleParameter_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        
        // Act
        var act = async () => await Connection.ExecuteScalarAsync<long>(SqlWithParam, _parameter1, cts.Token);
        
        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
    
    [Fact]
    public async Task ExecuteScalarSingleParameter_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        
        // Act
        var act = async () =>
        {
            await using var connectionB = new NpgsqlConnection(ConnectionString);
            await connectionB.ExecuteScalarAsync<int>(
                SqlWithParam, 
                _parameter1, 
                CancellationToken,
                commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<NpgsqlException>()).WithInnerException<TimeoutException>();
        await transaction.RollbackAsync(CancellationToken);
    }
    
    [Fact]
    public async Task ExecuteScalarSingleParameter_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);
        await Connection.ExecuteAsync("""TRUNCATE TABLE "public"."Users" """, CancellationToken, transaction: transaction);
        
        // Act
        var actual = await Connection.ExecuteScalarAsync<long>(
            SqlWithParam,
            _parameter1,
            CancellationToken,
            transaction: transaction);
        
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        actual.Should().Be(0);
    }
    
    [Fact]
    public async Task ExecuteScalarMultiParameter_ShouldReturnValue()
    {
        // Act
        var actual = await Connection.ExecuteScalarAsync<long>(SqlWithParam, [_parameter1], CancellationToken);

        // Assert
        actual.Should().Be((await GetAllUsers()).Count);
    }
    
    [Fact]
    public async Task ExecuteScalarMultiParameter_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        
        // Act
        var act = async () => await Connection.ExecuteScalarAsync<long>(SqlWithParam, [_parameter1], cts.Token);
        
        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
    
    [Fact]
    public async Task ExecuteScalarMultiParameter_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        
        // Act
        var act = async () =>
        {
            await using var connectionB = new NpgsqlConnection(ConnectionString);
            await connectionB.ExecuteScalarAsync<int>(
                SqlWithParam, 
                [_parameter1], 
                CancellationToken,
                commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<NpgsqlException>()).WithInnerException<TimeoutException>();
        await transaction.RollbackAsync(CancellationToken);
    }
    
    [Fact]
    public async Task ExecuteScalarMultiParameter_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);
        await Connection.ExecuteAsync("""TRUNCATE TABLE "public"."Users" """, CancellationToken, transaction: transaction);
        
        // Act
        var actual = await Connection.ExecuteScalarAsync<long>(
            SqlWithParam,
            [_parameter1],
            CancellationToken,
            transaction: transaction);
        
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        actual.Should().Be(0);
    }
}
using System.Data;
namespace AdoGen.SqlServer.Tests.Features;

public sealed class ExecuteScalarTests(TestContext testContext) : TestBase(testContext)
{
    private const string Sql = "SELECT COUNT(*) FROM Users";
    
    [Fact]
    public async Task ExecuteScalar_ShouldReturnValue()
    {
        // Act
        var actual = await Connection.ExecuteScalarAsync<int>(Sql, CancellationToken);

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
        var act = async () => await Connection.ExecuteScalarAsync<int>(Sql, cts.Token);
        
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
            await using var connectionB = new SqlConnection(ConnectionString);
            await connectionB.ExecuteScalarAsync<int>(Sql, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(-2);
        transaction.Rollback();
    }
    
    [Fact]
    public async Task ExecuteScalar_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = Connection.BeginTransaction();
        await Connection.ExecuteAsync("TRUNCATE TABLE Users", CancellationToken, transaction: transaction);
        
        // Act
        var actual = await Connection.ExecuteScalarAsync<int>(Sql, CancellationToken, transaction: transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        actual.Should().Be(0);
    }
    
    private const string SqlWithParam = "SELECT COUNT(*) FROM Users WHERE 1 = @p1";
    
    private readonly SqlParameter _parameter = new("@p1", 1);
    private readonly SqlParameter[] _parameters = [new("@p1", 1)];
    
    [Fact]
    public async Task ExecuteScalarSingleParameter_ShouldReturnValue()
    {
        // Act
        var actual = await Connection.ExecuteScalarAsync<int>(SqlWithParam, _parameter, CancellationToken);

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
        var act = async () => await Connection.ExecuteScalarAsync<int>(SqlWithParam, _parameter, cts.Token);
        
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
            await using var connectionB = new SqlConnection(ConnectionString);
            await connectionB.ExecuteScalarAsync<int>(SqlWithParam, _parameter, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(-2);
        transaction.Rollback();
    }
    
    [Fact]
    public async Task ExecuteScalarSingleParameter_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = Connection.BeginTransaction();
        await Connection.ExecuteAsync("TRUNCATE TABLE Users", CancellationToken, transaction: transaction);
        
        // Act
        var actual =
            await Connection.ExecuteScalarAsync<int>(
                SqlWithParam, 
                _parameter, 
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
        var actual = await Connection.ExecuteScalarAsync<int>(SqlWithParam, _parameters, CancellationToken);

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
        var act = async () => await Connection.ExecuteScalarAsync<int>(SqlWithParam, _parameters, cts.Token);
        
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
            await using var connectionB = new SqlConnection(ConnectionString);
            await connectionB.ExecuteScalarAsync<int>(SqlWithParam, _parameters, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(-2);
        transaction.Rollback();
    }
    
    [Fact]
    public async Task ExecuteScalarMultiParameter_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = Connection.BeginTransaction();
        await Connection.ExecuteAsync("TRUNCATE TABLE Users", CancellationToken, transaction: transaction);
        
        // Act
        var actual =
            await Connection.ExecuteScalarAsync<int>(
                SqlWithParam, 
                _parameters, 
                CancellationToken,
                transaction: transaction);
        
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        actual.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteScalar_ShouldReturnDefault_WhenNoRows()
    {
        // Act
        var actualInt = await Connection.ExecuteScalarAsync<int>("""SELECT 1 WHERE 1 = 0""", CancellationToken);
        var actualString = await Connection.ExecuteScalarAsync<string>("""SELECT Name FROM Users WHERE 1 = 0""", CancellationToken);

        // Assert
        actualInt.Should().Be(0);
        actualString.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteScalar_ShouldReturnDefault_WhenValueIsDbNull()
    {
        // Act
        var actualString = await Connection.ExecuteScalarAsync<string>("""SELECT MAX(Name) FROM Users WHERE 1 = 0""", CancellationToken);
        var actualInt = await Connection.ExecuteScalarAsync<int?>("""SELECT CAST(NULL AS INT)""", CancellationToken);

        // Assert
        actualString.Should().BeNull();
        actualInt.Should().BeNull();
    }

    [Fact]
    public async Task CreateParameter_ShouldSendDbNull_WhenValueIsNull()
    {
        // Arrange
        await using var cmd = new SqlCommand("SELECT CASE WHEN @p IS NULL THEN 1 ELSE 0 END", Connection);
        cmd.CreateParameter("@p", null, SqlDbType.NVarChar, 10);

        // Act
        var actual = await cmd.QueryScalarFirstOrDefaultAsync<int>(CancellationToken);

        // Assert
        actual.Should().Be(1);
    }


    [Fact]
    public async Task ExecuteScalar_ShouldConvertToSmallerNumericType_WhenProviderReturnsLong() =>
        (await Connection.ExecuteScalarAsync<int>("""SELECT COUNT_BIG(*) FROM Users""", CancellationToken)).Should().Be(DefaultUsers.Count);

    [Fact]
    public async Task ExecuteScalar_ShouldConvertToLargerNumericType_WhenProviderReturnsInt() =>
        (await Connection.ExecuteScalarAsync<long>("""SELECT COUNT(*) FROM Users""", CancellationToken)).Should().Be(DefaultUsers.Count);

    [Fact]
    public async Task ExecuteScalar_ShouldConvertToNullableNumericType_WhenProviderReturnsLong() =>
        (await Connection.ExecuteScalarAsync<int?>("""SELECT COUNT_BIG(*) FROM Users""", CancellationToken)).Should().Be(DefaultUsers.Count);

    [Fact]
    public async Task ExecuteScalar_ShouldReturnNull_WhenValueIsNull() =>
        (await Connection.ExecuteScalarAsync<int?>("SELECT CAST(NULL AS int)", CancellationToken)).Should().BeNull();

    [Fact]
    public async Task ExecuteScalar_ShouldThrowOverflowException_WhenValueDoesNotFit()
    {
        // Act
        var act = async () => await Connection.ExecuteScalarAsync<byte>("SELECT CAST(300 AS int)", CancellationToken);

        // Assert
        await act.Should().ThrowAsync<OverflowException>();
    }

    [Fact]
    public async Task ExecuteScalar_ShouldThrowInvalidCastException_WhenConversionIsNotNumeric()
    {
        // Act
        var act = async () => await Connection.ExecuteScalarAsync<Guid>("SELECT 1", CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidCastException>();
    }
}

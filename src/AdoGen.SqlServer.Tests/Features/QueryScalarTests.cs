using AdoGen.Sample.Features.Users;

namespace AdoGen.SqlServer.Tests.Features;

public sealed class QueryScalarTests(TestContext testContext) : TestBase(testContext)
{
    private const string SqlIds = "SELECT Id FROM Users ORDER BY Id";
    private const string SqlCount = "SELECT COUNT(*) FROM Users";
    private const string SqlNullable = "SELECT TOP(1) CAST(NULL AS NVARCHAR(50)) FROM Users";

    // ── QueryScalarAsync (no parameter) ──────────────────────────────────────

    [Fact]
    public async Task QueryScalar_ShouldReturnAllRows()
    {
        // Act
        var actual = await Connection.QueryScalarAsync<Guid>(SqlIds, CancellationToken);

        // Assert
        actual.Should().BeEquivalentTo(DefaultUsers.Select(x => x.Id));
    }

    [Fact]
    public async Task QueryScalar_ShouldReturnInt()
    {
        // Act
        var actual = await Connection.QueryScalarAsync<int>(SqlCount, CancellationToken);

        // Assert
        actual.Single().Should().Be(DefaultUsers.Count);
    }

    [Fact]
    public async Task QueryScalar_ShouldReturnDefaultForDbNull()
    {
        // Act
        var actual = await Connection.QueryScalarAsync<string>(SqlNullable, CancellationToken);

        // Assert
        actual.Single().Should().BeNull();
    }

    [Fact]
    public async Task QueryScalar_ShouldReturnEmptyList_WhenNoRows()
    {
        // Arrange
        await Connection.TruncateAsync<User>(CancellationToken);

        // Act
        var actual = await Connection.QueryScalarAsync<Guid>(SqlIds, CancellationToken);

        // Assert
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task QueryScalar_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = async () => await Connection.QueryScalarAsync<Guid>(SqlIds, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryScalar_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");

        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await connectionB.QueryScalarAsync<Guid>(SqlIds, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(-2);
        transaction.Rollback();
    }

    [Fact]
    public async Task QueryScalar_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = Connection.BeginTransaction();
        await Connection.TruncateAsync<User>(CancellationToken, transaction);

        // Act
        var actual = await Connection.QueryScalarAsync<Guid>(SqlIds, CancellationToken, transaction: transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        actual.Should().BeEmpty();
    }

    // ── QueryScalarAsync (single parameter) ──────────────────────────────────

    private const string SqlIdWithParam = "SELECT Id FROM Users WHERE Id = @id";

    [Fact]
    public async Task QueryScalarSingleParameter_ShouldReturnMatchingRow()
    {
        // Arrange
        var param = new SqlParameter("@id", DefaultUsers[0].Id);

        // Act
        var actual = await Connection.QueryScalarAsync<Guid>(SqlIdWithParam, param, CancellationToken);

        // Assert
        actual.Single().Should().Be(DefaultUsers[0].Id);
    }

    [Fact]
    public async Task QueryScalarSingleParameter_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var param = new SqlParameter("@id", DefaultUsers[0].Id);

        // Act
        var act = async () => await Connection.QueryScalarAsync<Guid>(SqlIdWithParam, param, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryScalarSingleParameter_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        var param = new SqlParameter("@id", DefaultUsers[0].Id);

        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await connectionB.QueryScalarAsync<Guid>(SqlIdWithParam, param, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(-2);
        transaction.Rollback();
    }

    [Fact]
    public async Task QueryScalarSingleParameter_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = Connection.BeginTransaction();
        await Connection.TruncateAsync<User>(CancellationToken, transaction);
        var param = new SqlParameter("@id", DefaultUsers[0].Id);

        // Act
        var actual = await Connection.QueryScalarAsync<Guid>(SqlIdWithParam, param, CancellationToken, transaction: transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        actual.Should().BeEmpty();
    }

    // ── QueryScalarAsync (multiple parameters) ────────────────────────────────

    private const string SqlIdsWithParams = "SELECT Id FROM Users WHERE Id = @id AND 1 = @one";

    private static SqlParameter[] MultiParams(Guid id) => [new("@id", id), new("@one", 1)];

    [Fact]
    public async Task QueryScalarMultiParameter_ShouldReturnMatchingRow()
    {
        // Act
        var actual = await Connection.QueryScalarAsync<Guid>(
            SqlIdsWithParams, 
            MultiParams(DefaultUsers[0].Id), 
            CancellationToken);

        // Assert
        actual.Single().Should().Be(DefaultUsers[0].Id);
    }

    [Fact]
    public async Task QueryScalarMultiParameter_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = async () => await Connection.QueryScalarAsync<Guid>(SqlIdsWithParams, MultiParams(DefaultUsers[0].Id), cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryScalarMultiParameter_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");

        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await connectionB.QueryScalarAsync<Guid>(SqlIdsWithParams, MultiParams(DefaultUsers[0].Id), CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(-2);
        transaction.Rollback();
    }

    [Fact]
    public async Task QueryScalarMultiParameter_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = Connection.BeginTransaction();
        await Connection.TruncateAsync<User>(CancellationToken, transaction);

        // Act
        var actual = await Connection.QueryScalarAsync<Guid>(SqlIdsWithParams, MultiParams(DefaultUsers[0].Id), CancellationToken, transaction: transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        actual.Should().BeEmpty();
    }

    // ── QueryScalarFirstOrDefaultAsync (no parameter) ─────────────────────────

    [Fact]
    public async Task QueryScalarFirstOrDefault_ShouldReturnFirstValue()
    {
        // Act
        var actual = await Connection.QueryScalarFirstOrDefaultAsync<int>(SqlCount, CancellationToken);

        // Assert
        actual.Should().Be(DefaultUsers.Count);
    }

    [Fact]
    public async Task QueryScalarFirstOrDefault_ShouldReturnEmpty_WhenNoRows()
    {
        // Arrange
        await Connection.TruncateAsync<User>(CancellationToken);

        // Act
        var actual = await Connection.QueryScalarFirstOrDefaultAsync<Guid>(SqlIds, CancellationToken);

        // Assert
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task QueryScalarFirstOrDefault_ShouldReturnNullForDbNull()
    {
        // Act
        var actual = await Connection.QueryScalarFirstOrDefaultAsync<string>(SqlNullable, CancellationToken);

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public async Task QueryScalarFirstOrDefault_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = async () => await Connection.QueryScalarFirstOrDefaultAsync<int>(SqlCount, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryScalarFirstOrDefault_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");

        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await connectionB.QueryScalarFirstOrDefaultAsync<int>(SqlCount, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(-2);
        transaction.Rollback();
    }

    [Fact]
    public async Task QueryScalarFirstOrDefault_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = Connection.BeginTransaction();
        await Connection.TruncateAsync<User>(CancellationToken, transaction);

        // Act
        var actual = await Connection.QueryScalarFirstOrDefaultAsync<int>(SqlCount, CancellationToken, transaction: transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        actual.Should().Be(0);
    }

    // ── QueryScalarFirstOrDefaultAsync (single parameter) ─────────────────────

    [Fact]
    public async Task QueryScalarFirstOrDefaultSingleParameter_ShouldReturnMatchingValue()
    {
        // Arrange
        var param = new SqlParameter("@id", DefaultUsers[0].Id);

        // Act
        Guid? actual = await Connection.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdWithParam, param, CancellationToken);

        // Assert
        actual.Should().Be(DefaultUsers[0].Id);
    }

    [Fact]
    public async Task QueryScalarFirstOrDefaultSingleParameter_ShouldReturnEmpty_WhenNoRows()
    {
        // Arrange
        var param = new SqlParameter("@id", Guid.NewGuid());

        // Act
        var actual = await Connection.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdWithParam, param, CancellationToken);

        // Assert
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task QueryScalarFirstOrDefaultSingleParameter_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var param = new SqlParameter("@id", DefaultUsers[0].Id);

        // Act
        var act = async () => await Connection.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdWithParam, param, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryScalarFirstOrDefaultSingleParameter_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        var param = new SqlParameter("@id", DefaultUsers[0].Id);

        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await connectionB.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdWithParam, param, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(-2);
        transaction.Rollback();
    }

    [Fact]
    public async Task QueryScalarFirstOrDefaultSingleParameter_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = Connection.BeginTransaction();
        await Connection.TruncateAsync<User>(CancellationToken, transaction);
        var param = new SqlParameter("@id", DefaultUsers[0].Id);

        // Act
        var actual = await Connection.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdWithParam, param, CancellationToken, transaction: transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        actual.Should().BeEmpty();
    }

    // ── QueryScalarFirstOrDefaultAsync (multiple parameters) ──────────────────

    [Fact]
    public async Task QueryScalarFirstOrDefaultMultiParameter_ShouldReturnMatchingValue()
    {
        // Arrange
        var expected = DefaultUsers[0].Id;

        // Act
        Guid? actual = await Connection.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdsWithParams, MultiParams(expected), CancellationToken);

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task QueryScalarFirstOrDefaultMultiParameter_ShouldReturnEmpty_WhenNoRows()
    {
        // Act
        var actual = await Connection.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdsWithParams, MultiParams(Guid.NewGuid()), CancellationToken);

        // Assert
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task QueryScalarFirstOrDefaultMultiParameter_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = async () => await Connection.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdsWithParams, MultiParams(DefaultUsers[0].Id), cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryScalarFirstOrDefaultMultiParameter_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");

        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await connectionB.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdsWithParams, MultiParams(DefaultUsers[0].Id), CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(-2);
        transaction.Rollback();
    }

    [Fact]
    public async Task QueryScalarFirstOrDefaultMultiParameter_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = Connection.BeginTransaction();
        await Connection.TruncateAsync<User>(CancellationToken, transaction);

        // Act
        var actual = await Connection.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdsWithParams, MultiParams(DefaultUsers[0].Id), CancellationToken, transaction: transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        actual.Should().BeEmpty();
    }
}


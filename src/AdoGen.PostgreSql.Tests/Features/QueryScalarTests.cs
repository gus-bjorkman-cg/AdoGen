using AdoGen.Sample.Features.Users;
using NpgsqlTypes;

namespace AdoGen.PostgreSql.Tests.Features;

public sealed class QueryScalarTests(TestContext testContext) : TestBase(testContext)
{
    private const string SqlIds = """SELECT "Id" FROM "public"."Users" ORDER BY "Id" """;
    private const string SqlCount = """SELECT COUNT(*) FROM "public"."Users" """;
    private const string SqlNullable = """SELECT NULL::text FROM "public"."Users" LIMIT 1""";

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
    public async Task QueryScalar_ShouldReturnLong()
    {
        // Act
        var actual = await Connection.QueryScalarAsync<long>(SqlCount, CancellationToken);

        // Assert
        actual.Should().ContainSingle().Which.Should().Be(DefaultUsers.Count);
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
    public async Task QueryScalar_ShouldThrowNpgsqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");

        // Act
        var act = async () =>
        {
            await using var connectionB = new NpgsqlConnection(ConnectionString);
            await connectionB.QueryScalarAsync<Guid>(SqlIds, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<NpgsqlException>()).WithInnerException<TimeoutException>();
        await transaction.RollbackAsync(CancellationToken);
    }

    [Fact]
    public async Task QueryScalar_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);
        await Connection.TruncateAsync<User>(CancellationToken, transaction);

        // Act
        var actual = await Connection.QueryScalarAsync<Guid>(SqlIds, CancellationToken, transaction: transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        actual.Should().BeEmpty();
    }

    // ── QueryScalarAsync (single parameter) ──────────────────────────────────

    private const string SqlIdWithParam = """SELECT "Id" FROM "public"."Users" WHERE "Id" = @id""";

    [Fact]
    public async Task QueryScalarSingleParameter_ShouldReturnMatchingRow()
    {
        // Arrange
        var param = new NpgsqlParameter<Guid>("id", DefaultUsers[0].Id);

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
        var param = new NpgsqlParameter<Guid>("id", DefaultUsers[0].Id);

        // Act
        var act = async () => await Connection.QueryScalarAsync<Guid>(SqlIdWithParam, param, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryScalarSingleParameter_ShouldThrowNpgsqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        var param = new NpgsqlParameter<Guid>("id", DefaultUsers[0].Id);

        // Act
        var act = async () =>
        {
            await using var connectionB = new NpgsqlConnection(ConnectionString);
            await connectionB.QueryScalarAsync<Guid>(SqlIdWithParam, param, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<NpgsqlException>()).WithInnerException<TimeoutException>();
        await transaction.RollbackAsync(CancellationToken);
    }

    [Fact]
    public async Task QueryScalarSingleParameter_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);
        await Connection.TruncateAsync<User>(CancellationToken, transaction);
        var param = new NpgsqlParameter<Guid>("id", DefaultUsers[0].Id);

        // Act
        var actual = await Connection.QueryScalarAsync<Guid>(SqlIdWithParam, param, CancellationToken, transaction: transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        actual.Should().BeEmpty();
    }

    // ── QueryScalarAsync (multiple parameters) ────────────────────────────────

    private const string SqlIdsWithParams = """SELECT "Id" FROM "public"."Users" WHERE "Id" = @id AND 1 = @one""";

    [Fact]
    public async Task QueryScalarMultiParameter_ShouldReturnMatchingRow()
    {
        // Arrange
        NpgsqlParameter[] parameters =
        [
            new NpgsqlParameter<Guid>("id", DefaultUsers[0].Id),
            new NpgsqlParameter<int>("one", 1) { NpgsqlDbType = NpgsqlDbType.Integer }
        ];

        // Act
        var actual = await Connection.QueryScalarAsync<Guid>(SqlIdsWithParams, parameters, CancellationToken);

        // Assert
        actual.Single().Should().Be(DefaultUsers[0].Id);
    }

    [Fact]
    public async Task QueryScalarMultiParameter_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        NpgsqlParameter[] parameters =
        [
            new NpgsqlParameter<Guid>("id", DefaultUsers[0].Id),
            new NpgsqlParameter<int>("one", 1) { NpgsqlDbType = NpgsqlDbType.Integer }
        ];

        // Act
        var act = async () => await Connection.QueryScalarAsync<Guid>(SqlIdsWithParams, parameters, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryScalarMultiParameter_ShouldThrowNpgsqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        NpgsqlParameter[] parameters =
        [
            new NpgsqlParameter<Guid>("id", DefaultUsers[0].Id),
            new NpgsqlParameter<int>("one", 1) { NpgsqlDbType = NpgsqlDbType.Integer }
        ];

        // Act
        var act = async () =>
        {
            await using var connectionB = new NpgsqlConnection(ConnectionString);
            await connectionB.QueryScalarAsync<Guid>(SqlIdsWithParams, parameters, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<NpgsqlException>()).WithInnerException<TimeoutException>();
        await transaction.RollbackAsync(CancellationToken);
    }

    [Fact]
    public async Task QueryScalarMultiParameter_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);
        await Connection.TruncateAsync<User>(CancellationToken, transaction);
        NpgsqlParameter[] parameters =
        [
            new NpgsqlParameter<Guid>("id", DefaultUsers[0].Id),
            new NpgsqlParameter<int>("one", 1) { NpgsqlDbType = NpgsqlDbType.Integer }
        ];

        // Act
        var actual = await Connection.QueryScalarAsync<Guid>(SqlIdsWithParams, parameters, CancellationToken, transaction: transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        actual.Should().BeEmpty();
    }

    // ── QueryScalarFirstOrDefaultAsync (no parameter) ─────────────────────────

    [Fact]
    public async Task QueryScalarFirstOrDefault_ShouldReturnFirstValue()
    {
        // Act
        var actual = await Connection.QueryScalarFirstOrDefaultAsync<long>(SqlCount, CancellationToken);

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
        var act = async () => await Connection.QueryScalarFirstOrDefaultAsync<long>(SqlCount, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryScalarFirstOrDefault_ShouldThrowNpgsqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");

        // Act
        var act = async () =>
        {
            await using var connectionB = new NpgsqlConnection(ConnectionString);
            await connectionB.QueryScalarFirstOrDefaultAsync<long>(SqlCount, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<NpgsqlException>()).WithInnerException<TimeoutException>();
        await transaction.RollbackAsync(CancellationToken);
    }

    [Fact]
    public async Task QueryScalarFirstOrDefault_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);
        await Connection.TruncateAsync<User>(CancellationToken, transaction);

        // Act
        var actual = await Connection.QueryScalarFirstOrDefaultAsync<long>(SqlCount, CancellationToken, transaction: transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        actual.Should().Be(0);
    }

    // ── QueryScalarFirstOrDefaultAsync (single parameter) ─────────────────────

    [Fact]
    public async Task QueryScalarFirstOrDefaultSingleParameter_ShouldReturnMatchingValue()
    {
        // Arrange
        var expected = DefaultUsers[0].Id;
        var param = new NpgsqlParameter<Guid>("id", expected);

        // Act
        Guid? actual = await Connection.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdWithParam, param, CancellationToken);

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task QueryScalarFirstOrDefaultSingleParameter_ShouldReturnEmpty_WhenNoRows()
    {
        // Arrange
        var param = new NpgsqlParameter<Guid>("id", Guid.NewGuid());

        // Act
        Guid? actual = await Connection.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdWithParam, param, CancellationToken);

        // Assert
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task QueryScalarFirstOrDefaultSingleParameter_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var param = new NpgsqlParameter<Guid>("id", DefaultUsers[0].Id);

        // Act
        var act = async () => await Connection.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdWithParam, param, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryScalarFirstOrDefaultSingleParameter_ShouldThrowNpgsqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        var param = new NpgsqlParameter<Guid>("id", DefaultUsers[0].Id);

        // Act
        var act = async () =>
        {
            await using var connectionB = new NpgsqlConnection(ConnectionString);
            await connectionB.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdWithParam, param, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<NpgsqlException>()).WithInnerException<TimeoutException>();
        await transaction.RollbackAsync(CancellationToken);
    }

    [Fact]
    public async Task QueryScalarFirstOrDefaultSingleParameter_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);
        await Connection.TruncateAsync<User>(CancellationToken, transaction);
        var param = new NpgsqlParameter<Guid>("id", DefaultUsers[0].Id);

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
        NpgsqlParameter[] parameters =
        [
            new NpgsqlParameter<Guid>("id", expected),
            new NpgsqlParameter<int>("one", 1) { NpgsqlDbType = NpgsqlDbType.Integer }
        ];

        // Act
        Guid? actual = await Connection.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdsWithParams, parameters, CancellationToken);

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task QueryScalarFirstOrDefaultMultiParameter_ShouldReturnEmpty_WhenNoRows()
    {
        // Arrange
        NpgsqlParameter[] parameters =
        [
            new NpgsqlParameter<Guid>("id", Guid.NewGuid()),
            new NpgsqlParameter<int>("one", 1) { NpgsqlDbType = NpgsqlDbType.Integer }
        ];

        // Act
        var actual = await Connection.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdsWithParams, parameters, CancellationToken);

        // Assert
        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task QueryScalarFirstOrDefaultMultiParameter_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        NpgsqlParameter[] parameters =
        [
            new NpgsqlParameter<Guid>("id", DefaultUsers[0].Id),
            new NpgsqlParameter<int>("one", 1) { NpgsqlDbType = NpgsqlDbType.Integer }
        ];

        // Act
        var act = async () => await Connection.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdsWithParams, parameters, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryScalarFirstOrDefaultMultiParameter_ShouldThrowNpgsqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        NpgsqlParameter[] parameters =
        [
            new NpgsqlParameter<Guid>("id", DefaultUsers[0].Id),
            new NpgsqlParameter<int>("one", 1) { NpgsqlDbType = NpgsqlDbType.Integer }
        ];

        // Act
        var act = async () =>
        {
            await using var connectionB = new NpgsqlConnection(ConnectionString);
            await connectionB.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdsWithParams, parameters, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<NpgsqlException>()).WithInnerException<TimeoutException>();
        await transaction.RollbackAsync(CancellationToken);
    }

    [Fact]
    public async Task QueryScalarFirstOrDefaultMultiParameter_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);
        await Connection.TruncateAsync<User>(CancellationToken, transaction);
        NpgsqlParameter[] parameters =
        [
            new NpgsqlParameter<Guid>("id", DefaultUsers[0].Id),
            new NpgsqlParameter<int>("one", 1) { NpgsqlDbType = NpgsqlDbType.Integer }
        ];

        // Act
        var actual = await Connection.QueryScalarFirstOrDefaultAsync<Guid>(SqlIdsWithParams, parameters, CancellationToken, transaction: transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        actual.Should().BeEmpty();
    }
}


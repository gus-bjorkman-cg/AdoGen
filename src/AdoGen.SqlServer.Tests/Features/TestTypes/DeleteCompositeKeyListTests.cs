using AdoGen.Sample.Features.TestTypes;
using Bogus;

namespace AdoGen.SqlServer.Tests.Features.TestTypes;

public sealed class DeleteCompositeKeyListTests(TestContext testContext) : TestBase(testContext)
{
    private static readonly Faker<TestType> Faker = Fakers.TestTypeFaker;

    private List<TestType> _seeded = [];

    protected override async ValueTask InitializeAsync()
    {
        var items = Faker.Generate(10);
        for (var i = 0; i < items.Count; i++)
            items[i] = items[i] with { Int = i + 1, Decimal = i + 1 };
        _seeded = items;
        await Connection.InsertAsync(_seeded, CancellationToken);
    }

    protected override async ValueTask DisposeAsync() => await Connection.TruncateAsync<TestType>(CancellationToken);

    private async ValueTask<List<TestType>> GetAll() =>
        await Connection.QueryAsync<TestType>("SELECT * FROM TestTypes", CancellationToken);

    [Fact]
    public async Task TestTypes_ShouldBeDeleted()
    {
        // Act
        await Connection.DeleteAsync(_seeded, CancellationToken);

        // Assert
        (await GetAll()).Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_ShouldOnlyRemoveMatchedKeys()
    {
        // Arrange — delete only the first 5
        var toDelete = _seeded.Take(5).ToList();
        var remaining = _seeded.Skip(5).ToList();

        // Act
        await Connection.DeleteAsync(toDelete, CancellationToken);

        // Assert
        (await GetAll()).Should().BeEquivalentTo(remaining, o => o.Excluding(x => x.CreatedAt));
    }

    [Fact]
    public async Task Delete_ShouldReturnZero_WhenListIsEmpty()
    {
        // Act
        var affected = await Connection.DeleteAsync(new List<TestType>(), CancellationToken);

        // Assert
        affected.Should().Be(0);
    }

    [Fact]
    public async Task Delete_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = async () => await Connection.DeleteAsync(_seeded, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Delete_ShouldThrowSqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("TestTypes");

        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await connectionB.DeleteAsync(_seeded, CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(-2);
        transaction.Rollback();
    }

    [Fact]
    public async Task Delete_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = Connection.BeginTransaction();

        // Act
        await Connection.DeleteAsync(_seeded, CancellationToken, transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        (await GetAll()).Should().HaveCount(10);
    }
}


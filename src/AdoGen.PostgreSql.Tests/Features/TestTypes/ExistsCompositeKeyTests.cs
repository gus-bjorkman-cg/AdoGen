using AdoGen.Sample.Features.TestTypes;
using Bogus;

namespace AdoGen.PostgreSql.Tests.Features.TestTypes;

public sealed class ExistsCompositeKeyTests(TestContext testContext) : TestBase(testContext)
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

    [Fact]
    public async Task Exists_ShouldReturnTrue_WhenTestTypeExists() => 
        (await Connection.ExistsAsync(_seeded[0], CancellationToken)).Should().BeTrue();

    [Fact]
    public async Task Exists_ShouldReturnFalse_WhenTestTypeDoesNotExist() => 
        (await Connection.ExistsAsync(_seeded[0] with { Int = 999 }, CancellationToken)).Should().BeFalse();

    [Fact]
    public async Task Exists_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = async () => await Connection.ExistsAsync(_seeded[0], cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Exists_ShouldThrowNpgsqlException_WhenCommandTimeoutIsReached()
    {
        // Arrange
        await using var transaction = await LockTable("TestTypes");

        // Act
        var act = async () =>
        {
            await using var connectionB = new NpgsqlConnection(ConnectionString);
            await connectionB.ExistsAsync(_seeded[0], CancellationToken, commandTimeout: 1);
        };

        // Assert
        (await act.Should().ThrowAsync<NpgsqlException>()).WithInnerException<TimeoutException>();
        await transaction.RollbackAsync(CancellationToken);
    }

    [Fact]
    public async Task Exists_ShouldRespectDbTransaction()
    {
        // Arrange
        var item = Faker.Generate() with { Int = 999, Decimal = 99 };
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);
        await Connection.InsertAsync(item, CancellationToken, transaction);

        // Act
        var withinTx = await Connection.ExistsAsync(item, CancellationToken, transaction);

        // Assert
        withinTx.Should().BeTrue();
    }
}

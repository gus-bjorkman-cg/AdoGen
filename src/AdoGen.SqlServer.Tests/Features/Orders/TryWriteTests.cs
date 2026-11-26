using AdoGen.Sample.Features.Orders;

namespace AdoGen.SqlServer.Tests.Features.Orders;

public sealed class TryWriteTests(TestContext testContext) : TestBase(testContext)
{
    private static Order NewOrder() => new(Guid.CreateVersion7(), "Missing", Guid.CreateVersion7(), 0);

    [Fact]
    public async Task TryUpdate_ShouldReturnApplied_WhenVersionMatches() =>
        (await Connection.TryUpdateAsync(DefaultOrders[0], CancellationToken)).Should().Be(SqlWriteResult.Applied);

    [Fact]
    public async Task TryUpdate_ShouldReturnConflict_WhenVersionIsStale()
    {
        // Arrange
        await Connection.UpdateAsync(DefaultOrders[0], CancellationToken);

        // Act
        var actual = await Connection.TryUpdateAsync(DefaultOrders[0], CancellationToken);

        // Assert
        actual.Should().Be(SqlWriteResult.Conflict);
    }

    [Fact]
    public async Task TryUpdate_ShouldReturnNotFound_WhenRowIsMissing() =>
        (await Connection.TryUpdateAsync(NewOrder(), CancellationToken)).Should().Be(SqlWriteResult.NotFound);

    [Fact]
    public async Task TryDelete_ShouldReturnApplied_WhenVersionMatches() =>
        (await Connection.TryDeleteAsync(DefaultOrders[0], CancellationToken)).Should().Be(SqlWriteResult.Applied);

    [Fact]
    public async Task TryDelete_ShouldReturnConflict_WhenVersionIsStale()
    {
        // Arrange
        await Connection.UpdateAsync(DefaultOrders[0], CancellationToken);

        // Act
        var actual = await Connection.TryDeleteAsync(DefaultOrders[0], CancellationToken);

        // Assert
        actual.Should().Be(SqlWriteResult.Conflict);
    }

    [Fact]
    public async Task TryDelete_ShouldReturnNotFound_WhenRowIsMissing() =>
        (await Connection.TryDeleteAsync(NewOrder(), CancellationToken)).Should().Be(SqlWriteResult.NotFound);

    [Fact]
    public async Task TryUpdate_ShouldReturnApplied_WhenModelHasNoConcurrencyToken() =>
        (await Connection.TryUpdateAsync(DefaultUsers[0] with { Name = "Updated" }, CancellationToken)).Should().Be(SqlWriteResult.Applied);

    [Fact]
    public async Task TryDelete_ShouldReturnNotFound_WhenModelHasNoConcurrencyTokenAndRowIsMissing() =>
        (await Connection.TryDeleteAsync(UserFaker.Generate(), CancellationToken)).Should().Be(SqlWriteResult.NotFound);
}

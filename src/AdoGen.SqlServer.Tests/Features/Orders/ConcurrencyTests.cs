using AdoGen.Sample.Features.Orders;

namespace AdoGen.SqlServer.Tests.Features.Orders;

public sealed class ConcurrencyTests(TestContext testContext) : TestBase(testContext)
{
    private Order2 _order2 = null!;

    protected override async ValueTask InitializeAsync()
    {
        _order2 = new Order2(Guid.CreateVersion7(), "Widget", Guid.CreateVersion7());
        await Connection.InsertAsync(_order2, CancellationToken);
    }

    protected override async ValueTask DisposeAsync() =>
        await Connection.TruncateAsync<Order2>(CancellationToken);

    // ── int version (Order) ──────────────────────────────────────────────────

    [Fact]
    public async Task Update_ShouldBumpVersion()
    {
        // Act
        await Connection.UpdateAsync(DefaultOrders[0], CancellationToken);

        // Assert
        var version = await Connection.QueryScalarFirstOrDefaultAsync<int>(
            "SELECT Version FROM [dbo].[Orders] WHERE Id = @Id",
            OrderSql.CreateParameterId(DefaultOrders[0].Id),
            CancellationToken);

        version.Should().Be(1);
    }

    [Fact]
    public async Task Delete_ShouldReturnOne_WhenCorrectVersion() =>
        (await Connection.DeleteAsync(DefaultOrders[0], CancellationToken)).Should().Be(1);

    [Fact]
    public async Task Update_ShouldThrowConcurrencyException_WhenStaleVersion()
    {
        // Arrange
        await Connection.UpdateAsync(DefaultOrders[0], CancellationToken);

        // Act
        var act = async () => await Connection.UpdateAsync(DefaultOrders[0], CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<AdoGenConcurrencyException>()).Which.TableName.Should().Be("dbo.Orders");
    }

    [Fact]
    public async Task Delete_ShouldThrowConcurrencyException_WhenStaleVersion()
    {
        // Arrange
        await Connection.UpdateAsync(DefaultOrders[0], CancellationToken);

        // Act
        var act = async () => await Connection.DeleteAsync(DefaultOrders[0], CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<AdoGenConcurrencyException>()).Which.TableName.Should().Be("dbo.Orders");
    }

    // ── Guid version (Order2) ────────────────────────────────────────────────

    [Fact]
    public async Task Update_ShouldSucceed_WhenVersionMatchesAndConcurrencyIsGuid()
    {
        // Act
        var affected = await Connection.UpdateAsync(_order2, CancellationToken);

        // Assert
        affected.Should().Be(1);
    }

    [Fact]
    public async Task Delete_ShouldSucceed_WhenVersionMatchesAndConcurrencyIsGuid() =>
        (await Connection.DeleteAsync(_order2, CancellationToken)).Should().Be(1);

    [Fact]
    public async Task Update_ShouldThrowConcurrencyException_WhenStaleVersionAndConcurrencyIsGuid()
    {
        await Connection.UpdateAsync(_order2, CancellationToken);

        var act = async () => await Connection.UpdateAsync(_order2, CancellationToken);

        (await act.Should().ThrowAsync<AdoGenConcurrencyException>()).Which.TableName.Should().Be("dbo.Order2s");
    }

    [Fact]
    public async Task Delete_ShouldThrowConcurrencyException_WhenStaleVersionAndConcurrencyIsGuid()
    {
        await Connection.UpdateAsync(_order2, CancellationToken);

        var act = async () => await Connection.DeleteAsync(_order2, CancellationToken);

        (await act.Should().ThrowAsync<AdoGenConcurrencyException>()).Which.TableName.Should().Be("dbo.Order2s");
    }

    // ── Patch with optimistic concurrency ────────────────────────────────────

    [Fact]
    public async Task Patch_ShouldBumpVersion_WhenExpectedVersionMatches()
    {
        // Arrange
        var patch = new OrderPatch(DefaultOrders[0].Id).WithProductName("Patched").WithExpectedVersion(0);

        // Act
        var affected = await Connection.PatchAsync(patch, CancellationToken);

        // Assert
        affected.Should().Be(1);
        var version = await Connection.QueryScalarFirstOrDefaultAsync<int>(
            "SELECT Version FROM [dbo].[Orders] WHERE Id = @Id",
            OrderSql.CreateParameterId(DefaultOrders[0].Id),
            CancellationToken);
        version.Should().Be(1);
    }

    [Fact]
    public async Task Patch_ShouldThrowConcurrencyException_WhenExpectedVersionIsStale()
    {
        // Arrange
        await Connection.UpdateAsync(DefaultOrders[0], CancellationToken);
        var patch = new OrderPatch(DefaultOrders[0].Id).WithProductName("Patched").WithExpectedVersion(0);

        // Act
        var act = async () => await Connection.PatchAsync(patch, CancellationToken);

        // Assert
        (await act.Should().ThrowAsync<AdoGenConcurrencyException>()).Which.TableName.Should().Be("dbo.Orders");
    }

    [Fact]
    public async Task Patch_ShouldReturnZero_WhenRowMissingAndNoExpectedVersion()
    {
        // Arrange
        var patch = new OrderPatch(Guid.CreateVersion7()).WithProductName("Patched");

        // Act
        var affected = await Connection.PatchAsync(patch, CancellationToken);

        // Assert
        affected.Should().Be(0);
    }

    [Fact]
    public async Task Patch_ShouldSucceedThenThrow_WhenGuidTokenIsReused()
    {
        // Arrange
        var next = Guid.CreateVersion7();
        var first = new Order2Patch(_order2.Id).WithProductName("A").WithExpectedVersion(_order2.Version, next);
        var stale = new Order2Patch(_order2.Id).WithProductName("B").WithExpectedVersion(_order2.Version, Guid.CreateVersion7());
        var fresh = new Order2Patch(_order2.Id).WithProductName("C").WithExpectedVersion(next, Guid.CreateVersion7());

        // Act
        var firstAffected = await Connection.PatchAsync(first, CancellationToken);
        var staleAct = async () => await Connection.PatchAsync(stale, CancellationToken);

        // Assert
        firstAffected.Should().Be(1);
        (await staleAct.Should().ThrowAsync<AdoGenConcurrencyException>()).Which.TableName.Should().Be("dbo.Order2s");
        (await Connection.PatchAsync(fresh, CancellationToken)).Should().Be(1);
    }
}

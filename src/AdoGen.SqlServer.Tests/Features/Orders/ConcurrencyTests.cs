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
}

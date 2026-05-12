using AdoGen.Sample.Features.Audit;
using AdoGen.Sample.Features.Orders;
using AwesomeAssertions.Execution;

namespace AdoGen.SqlServer.Tests.Features.Orders;

public sealed class InsertAndReturnTests(TestContext testContext) : TestBase(testContext)
{
    protected override async ValueTask DisposeAsync()
    {
        await Connection.TruncateAsync<AuditEvent>(CancellationToken);
    }

    [Fact]
    public async Task InsertAndReturn_ShouldReturnUser_WithMatchingValues()
    {
        // Arrange
        var user = UserFaker.Generate();

        // Act
        var returned = await Connection.InsertAndReturnAsync(user, CancellationToken);

        // Assert
        returned.Should().BeEquivalentTo(user);
    }

    [Fact]
    public async Task InsertAndReturn_ShouldReturnOrder_WithSameVersion()
    {
        // Arrange
        var order = new Order(Guid.CreateVersion7(), "WidgetPro", DefaultUsers[0].Id, 0);

        // Act
        var returned = await Connection.InsertAndReturnAsync(order, CancellationToken);

        // Assert
        returned.Should().BeEquivalentTo(order);
    }

    [Fact]
    public async Task InsertAndReturn_ShouldReturnAuditEvent_WithGeneratedIdentityKey()
    {
        // Arrange
        var auditEvent = Fakers.AuditEventFaker.Generate();

        // Act
        var returned = await Connection.InsertAndReturnAsync(auditEvent, CancellationToken);

        // Assert
        using var _ = new AssertionScope();
        returned.EventId.Should().BeGreaterThan(0, "the identity column should have been populated by SQL Server");
        returned.Should().BeEquivalentTo(auditEvent, e => e.Excluding(x => x.EventId));
    }

    [Fact]
    public async Task InsertAndReturn_ShouldRespectTransaction_Commit()
    {
        // Arrange
        var user = UserFaker.Generate();
        await using var tx = (SqlTransaction)await Connection.BeginTransactionAsync(CancellationToken);

        // Act
        var returned = await Connection.InsertAndReturnAsync(user, CancellationToken, tx);
        await tx.CommitAsync(CancellationToken);

        // Assert
        var fetched = await GetUser(returned.Id);
        fetched.Should().BeEquivalentTo(returned);
    }

    [Fact]
    public async Task InsertAndReturn_ShouldRespectTransaction_Rollback()
    {
        // Arrange
        var user = UserFaker.Generate();
        await using var tx = (SqlTransaction)await Connection.BeginTransactionAsync(CancellationToken);

        // Act
        await Connection.InsertAndReturnAsync(user, CancellationToken, tx);
        tx.Rollback();

        // Assert
        var fetched = await GetUser(user.Id);
        fetched.Should().BeNull("the transaction was rolled back");
    }
}

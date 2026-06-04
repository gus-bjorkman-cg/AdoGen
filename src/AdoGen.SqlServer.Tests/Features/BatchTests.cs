using AdoGen.Sample.Features.Orders;
using AdoGen.Sample.Features.Users;
using AwesomeAssertions.Execution;

namespace AdoGen.SqlServer.Tests.Features;

public sealed class BatchTests(TestContext testContext) : TestBase(testContext)
{
    private const string GetOrderSql = "SELECT * FROM Orders WHERE Id = @Id";

    private async ValueTask<Order?> GetOrder(Guid id) =>
        await Connection.QueryFirstOrDefaultAsync<Order>(GetOrderSql, OrderSql.CreateParameterId(id), CancellationToken);
    
    [Fact]
    public async Task Batch_ShouldInsertEntities()
    {
        // Arrange
        var user = UserFaker.Generate();
        await using var batch = new SqlBatch(Connection);
        batch.Insert(user);
        
        // Act
        await batch.ExecuteNonQueryAsync(CancellationToken);

        // Assert
        (await GetUser(user.Id)).Should().BeEquivalentTo(user);
    }

    [Fact]
    public async Task Batch_ShouldInsertMultipleEntityTypes()
    {
        // Arrange
        var user = UserFaker.Generate();
        var order = new Order(Guid.CreateVersion7(), "Batch Product", user.Id, 0);
        await using var batch = new SqlBatch(Connection);
        batch.Insert(user);
        batch.Insert(order);
        
        // Act
        await batch.ExecuteNonQueryAsync(CancellationToken);

        // Assert
        using var _ = new AssertionScope();
        (await GetUser(user.Id)).Should().BeEquivalentTo(user);
        (await GetOrder(order.Id)).Should().BeEquivalentTo(order);
    }

    [Fact]
    public async Task Batch_ShouldUpdateEntity()
    {
        // Arrange
        var updated = DefaultUsers[0] with { Name = "Updated via batch" };
        await using var batch = new SqlBatch(Connection);

        // Act
        batch.Update(updated);
        await batch.ExecuteNonQueryAsync(CancellationToken);

        // Assert
        (await GetUser(updated.Id)).Should().BeEquivalentTo(updated);
    }

    [Fact]
    public async Task Batch_ShouldDeleteEntity()
    {
        // Arrange
        var toDelete = DefaultUsers[0];
        await using var batch = new SqlBatch(Connection);
        batch.Delete(toDelete);

        // Act
        await batch.ExecuteNonQueryAsync(CancellationToken);

        // Assert
        (await GetUser(toDelete.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Batch_ShouldUpsertEntity_WhenRowExists()
    {
        // Arrange
        var upserted = DefaultUsers[0] with { Name = "Upserted via batch" };
        await using var batch = new SqlBatch(Connection);
        batch.Upsert(upserted);

        // Act
        await batch.ExecuteNonQueryAsync(CancellationToken);

        // Assert
        (await GetUser(upserted.Id)).Should().BeEquivalentTo(upserted);
    }

    [Fact]
    public async Task Batch_ShouldUpsertEntity_WhenRowDoesNotExist()
    {
        // Arrange
        var newUser = UserFaker.Generate();
        await using var batch = new SqlBatch(Connection);
        batch.Upsert(newUser);
        
        // Act
        await batch.ExecuteNonQueryAsync(CancellationToken);

        // Assert
        (await GetUser(newUser.Id)).Should().BeEquivalentTo(newUser);
    }
    
    [Fact]
    public async Task BatchInsertAndReturn_ShouldReturnEntity()
    {
        // Arrange
        var user = UserFaker.Generate();
        await using var batch = new SqlBatch(Connection);
        batch.InsertAndReturn(user);

        // Act
        await using var reader = await batch.ExecuteReaderAsync(CancellationToken);
        await reader.ReadAsync(CancellationToken);
        var returned = User.Map(reader);

        // Assert
        returned.Should().BeEquivalentTo(user);
    }
    
    [Fact]
    public async Task BatchInsertAndReturn_ShouldInsertEntity()
    {
        // Arrange
        var user = UserFaker.Generate();
        await using var batch = new SqlBatch(Connection);
        batch.InsertAndReturn(user);

        // Act
        await batch.ExecuteNonQueryAsync(CancellationToken);
        
        // Assert
        (await GetUser(user.Id)).Should().BeEquivalentTo(user);
    }

    [Fact]
    public async Task Batch_ShouldInsertAndReturnMultipleEntities()
    {
        // Arrange
        var user = UserFaker.Generate();
        var order = new Order(Guid.CreateVersion7(), "Batch return order", user.Id, 0);
        await using var batch = new SqlBatch(Connection);
        batch.InsertAndReturn(user);
        batch.InsertAndReturn(order);

        // Act
        await using var reader = await batch.ExecuteReaderAsync(CancellationToken);
        
        await reader.ReadAsync(CancellationToken);
        var returnedUser = User.Map(reader);
        
        await reader.NextResultAsync(CancellationToken);
        await reader.ReadAsync(CancellationToken);
        var returnedOrder = Order.Map(reader);

        // Assert
        using var _ = new AssertionScope();
        returnedUser.Should().BeEquivalentTo(user);
        returnedOrder.Should().BeEquivalentTo(order);
    }

    [Fact]
    public async Task Batch_ShouldPerformMixedOperationsOnMultipleEntityTypes()
    {
        // Arrange
        var newUser = UserFaker.Generate();
        var updatedUser = DefaultUsers[0] with { Name = "Batch mixed update" };
        var deletedUser = DefaultUsers[1];
        var order = new Order(Guid.CreateVersion7(), "Mixed batch order", newUser.Id, 0);
        Order returnedOrder;

        await using var batch = new SqlBatch(Connection);
        batch.Insert(newUser);
        batch.Update(updatedUser);
        batch.Delete(deletedUser);
        batch.InsertAndReturn(order);

        // Act
        
        // SqlBatch: non-returning DML commands produce no result sets in the reader.
        // InsertAndReturn (OUTPUT INSERTED.*) is the only row-returning command, so it is on result set 0.
        await using (var reader = await batch.ExecuteReaderAsync(CancellationToken))
        {
            await reader.ReadAsync(CancellationToken);
            returnedOrder = Order.Map(reader);
        }
        
        // Assert
        using var _ = new AssertionScope();
        returnedOrder.Should().BeEquivalentTo(order);
        (await GetUser(newUser.Id)).Should().BeEquivalentTo(newUser);
        (await GetUser(updatedUser.Id)).Should().BeEquivalentTo(updatedUser);
        (await GetUser(deletedUser.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Batch_ShouldRespectDbTransaction_WhenRolledBack()
    {
        // Arrange
        var user = UserFaker.Generate();
        await using var transaction = Connection.BeginTransaction();
        await using var batch = new SqlBatch(Connection, transaction);
        batch.Insert(user);
        
        // Act
        await batch.ExecuteNonQueryAsync(CancellationToken);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        (await GetUser(user.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Batch_ShouldRespectDbTransaction_WhenCommitted()
    {
        // Arrange
        var user = UserFaker.Generate();
        await using var transaction = Connection.BeginTransaction();
        await using var batch = new SqlBatch(Connection, transaction);

        // Act
        batch.Insert(user);
        await batch.ExecuteNonQueryAsync(CancellationToken);
        await transaction.CommitAsync(CancellationToken);

        // Assert
        (await GetUser(user.Id)).Should().BeEquivalentTo(user);
    }

    [Fact]
    public async Task Batch_ShouldAllowMixingWithCustomBatchCommands()
    {
        // Arrange
        var user = UserFaker.Generate();
        await using var batch = new SqlBatch(Connection);
        batch.Insert(user);
        batch.BatchCommands.Add(new SqlBatchCommand($"UPDATE Users SET Name = 'custom' WHERE Id = '{user.Id}'"));

        // Act
        await batch.ExecuteNonQueryAsync(CancellationToken);
        
        // Assert
        (await GetUser(user.Id)).Should().BeEquivalentTo(user with {Name = "custom"});
    }

    [Fact]
    public async Task Batch_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        var user = UserFaker.Generate();
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = async () =>
        {
            await using var batch = new SqlBatch(Connection);
            batch.Insert(user);
            await batch.ExecuteNonQueryAsync(cts.Token);
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

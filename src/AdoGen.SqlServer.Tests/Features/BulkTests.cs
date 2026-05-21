using AdoGen.Sample.Features.Users;
using AwesomeAssertions.Execution;

namespace AdoGen.SqlServer.Tests.Features;

public sealed class BulkTests(TestContext testContext) : TestBase(testContext)
{
    private readonly UserBulk _bulk = new();
    
    [Fact]
    public async Task BulkAdd_ShouldInsertEntities()
    {
        // Arrange
        var users = UserFaker.Generate(10);
        _bulk.AddRange(users);
        await using var transaction = Connection.BeginTransaction();

        // Act
        await _bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);

        // Assert
        (await GetAllUsers()).Should().BeEquivalentTo(users.Concat(DefaultUsers));
    }

    [Fact]
    public async Task BulkAdd_ShouldReturnCorrectCount()
    {
        // Arrange
        _bulk.AddRange(UserFaker.Generate(10));
        await using var transaction = Connection.BeginTransaction();

        // Act
        var actual = await _bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);

        // Assert
        actual.Should().Be(new BulkApplyResult(10, 0, 0, 0));
    }

    [Fact]
    public async Task BulkUpdate_ShouldUpdateEntities()
    {
        // Arrange
        var users = DefaultUsers.Select((t, i) => t with { Name = $"other name {i}" }).ToList();
        _bulk.UpdateRange(users);
        await using var transaction = Connection.BeginTransaction();

        // Act
        await _bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);

        // Assert
        (await GetAllUsers()).Should().BeEquivalentTo(users);
    }

    [Fact]
    public async Task BulkUpdate_ShouldReturnCorrectCount()
    {
        // Arrange
        var users = DefaultUsers.Select((t, i) => t with { Name = $"other name {i}" }).ToList();
        _bulk.UpdateRange(users);
        await using var transaction = Connection.BeginTransaction();

        // Act
        var actual = await _bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);

        // Assert
        actual.Should().Be(new BulkApplyResult(0, users.Count, 0, 0));
    }

    [Fact]
    public async Task BulkDelete_ShouldDeleteEntities()
    {
        // Arrange
        _bulk.RemoveRange(DefaultUsers);
        await using var transaction = Connection.BeginTransaction();

        // Act
        await _bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);

        // Assert
        (await GetAllUsers()).Should().BeEmpty();
    }

    [Fact]
    public async Task BulkDelete_ShouldReturnCorrectCount()
    {
        // Arrange
        _bulk.RemoveRange(DefaultUsers);
        await using var transaction = Connection.BeginTransaction();

        // Act
        var actual = await _bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);

        // Assert
        actual.Should().Be(new BulkApplyResult(0, 0, DefaultUsers.Count, 0));
    }

    [Fact]
    public async Task BulkMixed_ShouldPerformAllOperations()
    {
        // Arrange
        var usersToAdd = UserFaker.Generate(5);
        var usersToUpsert = UserFaker.Generate(5);
        var usersToUpdate = DefaultUsers.Take(5).Select((t, i) => t with { Name = $"other name {i}" }).ToList();
        var usersToDelete = DefaultUsers.Skip(5).Take(5).ToList();
        
        _bulk.AddRange(usersToAdd);
        _bulk.UpsertRange(usersToUpsert);
        _bulk.UpdateRange(usersToUpdate);
        _bulk.RemoveRange(usersToDelete);
        
        await using var transaction = Connection.BeginTransaction();

        // Act
        await _bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);

        // Assert
        (await GetAllUsers()).Should().BeEquivalentTo(usersToAdd.Concat(usersToUpdate).Concat(usersToUpsert));
    }

    [Fact]
    public async Task BulkMixed_ShouldReturnCorrectCount()
    {
        // Arrange
        var usersToAdd = UserFaker.Generate(7);
        var usersToUpsert = UserFaker.Generate(6);
        var usersToUpdate = DefaultUsers.Take(5).Select((t, i) => t with { Name = $"other name {i}" }).ToList();
        var usersToDelete = DefaultUsers.Skip(5).Take(4).ToList();
        
        _bulk.AddRange(usersToAdd);
        _bulk.UpsertRange(usersToUpsert);
        _bulk.UpdateRange(usersToUpdate);
        _bulk.RemoveRange(usersToDelete);
        
        await using var transaction = Connection.BeginTransaction();

        // Act
        var actual = await _bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);

        // Assert
        actual.Should().Be(new BulkApplyResult(usersToAdd.Count, usersToUpdate.Count, usersToDelete.Count,
            usersToUpsert.Count));
    }
    
    [Fact]
    public async Task Bulk_ShouldUseTransaction()
    {
        // Arrange
        var users = UserFaker.Generate(10);
        _bulk.AddRange(users);
        await using var transaction = Connection.BeginTransaction();
        
        // Act
        await _bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.RollbackAsync(CancellationToken);
        
        // Assert
        (await GetAllUsers()).Should().BeEquivalentTo(DefaultUsers);
    }

    [Fact]
    public async Task Bulk_ShouldThrowOperationCanceledException_WhenCtsIsCancelled()
    {
        // Arrange
        _bulk.AddRange(UserFaker.Generate(10));
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        
        // Act
        var act = async () =>
        {
            await using var transaction = Connection.BeginTransaction();
            await _bulk.SaveChangesAsync(Connection, transaction, cts.Token);
        };
        
        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
    
    [Fact]
    public void BulkOp_Insert_ShouldHaveValueI() => BulkOp.Insert.Value.Should().Be('I');
    
    [Fact]
    public void BulkOp_Update_ShouldHaveValueU() => BulkOp.Update.Value.Should().Be('U');
    
    [Fact]
    public void BulkOp_Delete_ShouldHaveValueD() => BulkOp.Delete.Value.Should().Be('D');
    
    [Fact]
    public void BulkOp_Upsert_ShouldHaveValueM() => BulkOp.Upsert.Value.Should().Be('M');

    [Fact]
    public void BulkBatch_HasInserts_ShouldBeTrueAfterInsert()
    {
        // Act
        _bulk.Add(DefaultUsers[0]);

        // Assert
        using var _ = new AssertionScope();
        _bulk.HasUpserts.Should().BeFalse();
        _bulk.HasInserts.Should().BeTrue();
        _bulk.HasUpdates.Should().BeFalse();
        _bulk.HasDeletes.Should().BeFalse();
    }
    
    [Fact]
    public void BulkBatch_HasUpdate_ShouldBeTrueAfterUpdate()
    {
        // Act
        _bulk.Update(DefaultUsers[0]);

        // Assert
        using var _ = new AssertionScope();
        _bulk.HasUpserts.Should().BeFalse();
        _bulk.HasInserts.Should().BeFalse();
        _bulk.HasUpdates.Should().BeTrue();
        _bulk.HasDeletes.Should().BeFalse();
    }
    
    [Fact]
    public void BulkBatch_HasDelete_ShouldBeTrueAfterDelete()
    {
        // Act
        _bulk.Remove(DefaultUsers[0]);

        // Assert
        using var _ = new AssertionScope();
        _bulk.HasUpserts.Should().BeFalse();
        _bulk.HasInserts.Should().BeFalse();
        _bulk.HasUpdates.Should().BeFalse();
        _bulk.HasDeletes.Should().BeTrue();
    }
    
    [Fact]
    public void BulkBatch_HasUpserts_ShouldBeTrueAfterUpsert()
    {
        // Act
        _bulk.Upsert(DefaultUsers[0]);

        // Assert
        using var _ = new AssertionScope();
        _bulk.HasUpserts.Should().BeTrue();
        _bulk.HasInserts.Should().BeFalse();
        _bulk.HasUpdates.Should().BeFalse();
        _bulk.HasDeletes.Should().BeFalse();
    }

    [Fact]
    public async Task BulkUpsert_ShouldInsertNewRowsAndUpdateExistingRows_WhenMixedUpsertBatch()
    {
        // Arrange
        var existingUsersWithChanges = DefaultUsers.Take(3).Select((t, i) => t with { Name = $"other name {i}" }).ToList();
        var newUsers = UserFaker.Generate(2);
        
        _bulk.UpsertRange(existingUsersWithChanges);
        _bulk.UpsertRange(newUsers);
        _bulk.RemoveRange(DefaultUsers.Skip(3));
        
        await using var transaction = Connection.BeginTransaction();

        // Act
        await _bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);

        // Assert
        (await GetAllUsers()).Should().BeEquivalentTo(existingUsersWithChanges.Concat(newUsers));
    }
}
using AdoGen.Sample.Features.Users;

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
        actual.Should().Be(new BulkApplyResult(10, 0, 0));
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
        actual.Should().Be(new BulkApplyResult(0, users.Count, 0));
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
        actual.Should().Be(new BulkApplyResult(0, 0, DefaultUsers.Count));
    }
    
    [Fact]
    public async Task BulkMixed_ShouldPerformAllOperations()
    {
        // Arrange
        var usersToAdd = UserFaker.Generate(5);
        var usersToUpdate = DefaultUsers.Take(5).Select((t, i) => t with { Name = $"other name {i}" }).ToList();
        var usersToDelete = DefaultUsers.Skip(5).Take(5).ToList();
        _bulk.AddRange(usersToAdd);
        _bulk.UpdateRange(usersToUpdate);
        _bulk.RemoveRange(usersToDelete);
        await using var transaction = Connection.BeginTransaction();
        
        // Act
        await _bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);
        
        // Assert
        (await GetAllUsers()).Should().BeEquivalentTo(usersToAdd.Concat(usersToUpdate));
    }
    
    [Fact]
    public async Task BulkMixed_ShouldReturnCorrectCount()
    {
        // Arrange
        var usersToAdd = UserFaker.Generate(5);
        var usersToUpdate = DefaultUsers.Take(5).Select((t, i) => t with { Name = $"other name {i}" }).ToList();
        var usersToDelete = DefaultUsers.Skip(5).Take(5).ToList();
        _bulk.AddRange(usersToAdd);
        _bulk.UpdateRange(usersToUpdate);
        _bulk.RemoveRange(usersToDelete);
        await using var transaction = Connection.BeginTransaction();
        
        // Act
        var actual = await _bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);
        
        // Assert
        actual.Should().Be(new BulkApplyResult(usersToAdd.Count, usersToUpdate.Count, usersToDelete.Count));
    }
}
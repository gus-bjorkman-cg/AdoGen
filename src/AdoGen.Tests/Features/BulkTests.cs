using AdoGen.Abstractions;
using AdoGen.Sample.Features.Users;

namespace AdoGen.Tests.Features;

public sealed class BulkTests(TestContext testContext) : TestBase(testContext)
{
    private readonly UserBulk _bulk = new();
    
    [Fact]
    public async Task BulkAdd()
    {
        // Arrange
        await Connection.TruncateAsync<User>(CancellationToken);
        var users = UserFaker.Generate(10);
        _bulk.AddRange(users);
        await using var transaction = Connection.BeginTransaction(); 
        
        // Act
        await _bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);
        
        // Assert
        var actual = await Connection.QueryAsync<User>("SELECT * FROM Users", CancellationToken);
        actual.Should().BeEquivalentTo(users);
    }

    [Fact]
    public async Task BulkUpdate()
    {
        // Arrange
        var users = DefaultUsers.Select((t, i) => t with { Name = $"other name {i}" }).ToList();
        _bulk.UpdateRange(users);
        await using var transaction = Connection.BeginTransaction();
        
        // Act
        await _bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);
        
        // Assert
        var actual = await Connection.QueryAsync<User>("SELECT * FROM Users", CancellationToken);
        actual.Should().BeEquivalentTo(users);
    }
    
    [Fact]
    public async Task BulkDelete()
    {
        // Arrange
        _bulk.RemoveRange(DefaultUsers);
        await using var transaction = Connection.BeginTransaction();
        
        // Act
        await _bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);
        
        // Assert
        var actual = await Connection.QueryAsync<User>("SELECT * FROM Users", CancellationToken);
        actual.Should().BeEmpty();
    }
    
    [Fact]
    public async Task BulkMixed()
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
        var actual = await Connection.QueryAsync<User>("SELECT * FROM Users", CancellationToken);
        actual.Should().BeEquivalentTo(usersToAdd.Concat(usersToUpdate));
    }
}
using AdoGen.Sample.Features.Users.Commands;

namespace AdoGen.Tests.Features.Users.Commands;

public sealed class UpdateUsersBulkCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly UpdateUsersBulkCommandHandler _sut = new(testContext.ConnectionString);
    
    [Fact]
    public async Task UpdateUsersBulkCommandHandler_ShouldUpdateUsers()
    {
        // Arrange
        var users = DefaultUsers.Select((x, i) => x with { Name = i.ToString() }).ToList();
        
        // Act
        await _sut.Handle(new UpdateUsersBulkCommand(users), CancellationToken);
        
        // Assert
        (await GetAllUsers()).Should().BeEquivalentTo(users);
    }
}
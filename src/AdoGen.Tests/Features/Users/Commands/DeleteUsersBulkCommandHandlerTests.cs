using AdoGen.Sample.Features.Users.Commands;

namespace AdoGen.Tests.Features.Users.Commands;

public sealed class DeleteUsersBulkCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly DeleteUsersBulkCommandHandler _sut = new(testContext.ConnectionString);

    [Fact]
    public async Task DeleteUsersBulkCommandHandler_ShouldDeleteUsers()
    {
        // Act
        await _sut.Handle(new DeleteUsersBulkCommand(DefaultUsers), CancellationToken);
        
        // Assert
        (await GetAllUsers()).Should().BeEmpty();
    }
}
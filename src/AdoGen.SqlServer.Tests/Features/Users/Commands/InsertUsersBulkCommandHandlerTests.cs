using AdoGen.Sample.Features.Users.Commands;

namespace AdoGen.SqlServer.Tests.Features.Users.Commands;

public sealed class InsertUsersBulkCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly InsertUsersBulkCommandHandler _sut = new(testContext.ConnectionString);

    [Fact]
    public async Task InsertUsersBulkCommandHandler_ShouldInsertUsers()
    {
        // Arrange
        var users = UserFaker.Generate(10);
        
        // Act
        await _sut.SqlServer(new InsertUsersBulkCommand(users), CancellationToken);
        
        // Assert
        (await GetAllUsers()).Should().BeEquivalentTo(users.Concat(DefaultUsers));
    }
}
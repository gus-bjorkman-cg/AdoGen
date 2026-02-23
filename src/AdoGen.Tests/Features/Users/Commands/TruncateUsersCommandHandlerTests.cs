using AdoGen.Sample.Features.Users.Commands;

namespace AdoGen.Tests.Features.Users.Commands;

public sealed class TruncateUsersCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly TruncateUsersCommandHandler _sut = new(testContext.ConnectionString);

    [Fact]
    public async Task TruncateUsersCommandHandler_ShouldTruncateUsers()
    {
        // Act
        await _sut.Handle(TruncateUsersCommand.Instance, CancellationToken);
        
        // Assert
        (await GetAllUsers()).Should().BeEmpty();
    }
}
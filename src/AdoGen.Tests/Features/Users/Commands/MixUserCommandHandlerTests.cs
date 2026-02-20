using AdoGen.Sample.Features.Users.Commands;

namespace AdoGen.Tests.Features.Users.Commands;

public sealed class MixUserCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly MixUserCommandHandler _sut = new(testContext.ConnectionString);

    [Fact]
    public async Task MixUserCommandHandler_ShouldMixUsers()
    {
        // Arrange
        var usersToInsert = UserFaker.Generate(10).ToList();
        var usersToUpdate = DefaultUsers.Take(5).Select((x, i) => x with { Name = i.ToString() }).ToList();
        var usersToDelete = DefaultUsers.Skip(5).ToList();

        // Act
        await _sut.Handle(new MixUserCommand(usersToInsert, usersToUpdate, usersToDelete), CancellationToken);

        // Assert
        (await GetAllUsers()).Should().BeEquivalentTo(usersToInsert.Concat(usersToUpdate));
    }
}
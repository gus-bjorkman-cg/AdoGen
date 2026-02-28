using AdoGen.Sample.Features.Users.Commands;

namespace AdoGen.PostgreSql.Tests.Features.Users.Commands;

public sealed class DeleteUsersNpgsqlCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly DeleteUsersCommandHandler _sut = new(testContext.ConnectionString);

    [Fact]
    public async Task DeleteUsersCommandHandler_ShouldDeleteAllUsers()
    {
        // Act
        await _sut.NpgSql(new DeleteUsersCommand(DefaultUsers.Select(x => x.Id).ToList()), CancellationToken);

        // Assert
        (await GetAllUsers()).Should().BeEmpty();
    }
}


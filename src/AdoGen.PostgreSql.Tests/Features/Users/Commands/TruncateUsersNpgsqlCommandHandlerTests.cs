using AdoGen.Sample.Features.Users.Commands;

namespace AdoGen.PostgreSql.Tests.Features.Users.Commands;

public sealed class TruncateUsersNpgsqlCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly TruncateUsersCommandHandler _sut = new(testContext.ConnectionString);

    [Fact]
    public async Task TruncateUsersCommandHandler_ShouldTruncateUsers()
    {
        // Act
        await _sut.NpgSql(TruncateUsersCommand.Instance, CancellationToken);

        // Assert
        (await GetAllUsers()).Should().BeEmpty();
    }
}


using AdoGen.Sample.Features.Users.Commands;

namespace AdoGen.PostgreSql.Tests.Features.Users.Commands;

public sealed class InsertUsersNpgsqlCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly InsertUsersCommandHandler _sut = new(testContext.ConnectionString);

    [Fact]
    public async Task InsertUsersCommandHandler_ShouldInsertAllUsers()
    {
        // Arrange
        var users = UserFaker.Generate(10);

        // Act
        await _sut.NpgSql(new InsertUsersCommand(users), CancellationToken);

        // Assert
        (await GetAllUsers()).Should().BeEquivalentTo(users.Concat(DefaultUsers));
    }
}


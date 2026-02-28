using AdoGen.Sample.Features.Users.Commands;

namespace AdoGen.PostgreSql.Tests.Features.Users.Commands;

public sealed class UpdateUsersBulkNpgsqlCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly UpdateUsersBulkCommandHandler _sut = new(testContext.ConnectionString);

    [Fact]
    public async Task UpdateUsersBulkCommandHandler_ShouldUpdateUsers()
    {
        // Arrange
        var users = DefaultUsers.Select((x, i) => x with { Name = i.ToString() }).ToList();

        // Act
        await _sut.NpgSql(new UpdateUsersBulkCommand(users), CancellationToken);

        // Assert
        (await GetAllUsers()).Should().BeEquivalentTo(users);
    }
}


using AdoGen.Sample.Features.Users.Commands;

namespace AdoGen.PostgreSql.Tests.Features.Users.Commands;

public sealed class DeleteUsersBulkNpgsqlCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly DeleteUsersBulkCommandHandler _sut = new(testContext.ConnectionString);

    [Fact]
    public async Task DeleteUsersBulkCommandHandler_ShouldDeleteUsers()
    {
        // Act
        await _sut.NpgSql(new DeleteUsersBulkCommand(DefaultUsers), CancellationToken);

        // Assert
        (await GetAllUsers()).Should().BeEmpty();
    }
}


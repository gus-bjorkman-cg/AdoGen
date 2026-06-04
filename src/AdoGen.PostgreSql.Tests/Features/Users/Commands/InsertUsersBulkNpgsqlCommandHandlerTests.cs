using AdoGen.Sample.Features.Users.Commands;

namespace AdoGen.PostgreSql.Tests.Features.Users.Commands;

public sealed class InsertUsersBulkNpgsqlCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly InsertUsersBulkCommandHandler _sut = new(testContext.ConnectionString);

    [Fact]
    public async Task InsertUsersBulkCommandHandler_ShouldInsertUsers()
    {
        // Arrange
        var users = UserFaker.Generate(10);

        // Act
        await _sut.NpgSql(new InsertUsersBulkCommand(users), CancellationToken);

        // Assert
        (await GetAllUsers()).Should().BeEquivalentTo(users.Concat(DefaultUsers));
    }
}


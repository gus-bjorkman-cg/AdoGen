using AdoGen.Sample.Features.Users;
using AdoGen.Sample.Features.Users.Commands;

namespace AdoGen.PostgreSql.Tests.Features.Users.Commands;

public sealed class DeleteUserNpgsqlCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly DeleteUserCommandHandler _sut = new(testContext.ConnectionString);

    private const string SqlSelectById = """SELECT * FROM "public"."Users" WHERE "Id" = @Id""";

    [Fact]
    public async Task DeleteUserCommand_ShouldDeleteUser()
    {
        // Arrange
        var id = DefaultUsers[0].Id;

        // Act
        await _sut.NpgSql(new DeleteUserCommand(id), CancellationToken);

        // Assert
        var user = await Connection.QueryFirstOrDefaultAsync<User>(SqlSelectById,
            UserNpgsql.CreateParameterId(id), CancellationToken);
        user.Should().BeNull();
    }
}


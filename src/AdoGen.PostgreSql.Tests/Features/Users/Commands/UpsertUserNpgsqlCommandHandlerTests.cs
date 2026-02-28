using AdoGen.Sample.Features.Users;
using AdoGen.Sample.Features.Users.Commands;

namespace AdoGen.PostgreSql.Tests.Features.Users.Commands;

public sealed class UpsertUserNpgsqlCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly UpsertUserCommandHandler _sut = new(testContext.ConnectionString);
    private readonly User _user = UserFaker.Generate();

    private const string Sql = """SELECT * FROM "public"."Users" WHERE "Id" = @Id""";

    [Fact]
    public async Task User_ShouldBeCreated_WhenNotExisting()
    {
        // Act
        await _sut.NpgSql(new UpsertUserCommand(_user), CancellationToken);

        // Assert
        var user = await Connection.QueryFirstOrDefaultAsync<User>(Sql,
            UserNpgsql.CreateParameterId(_user.Id), CancellationToken);
        user.Should().Be(_user);
    }

    [Fact]
    public async Task User_ShouldBeUpdated_WhenExisting()
    {
        // Arrange
        var user = _user with { Id = DefaultUsers[0].Id };

        // Act
        await _sut.NpgSql(new UpsertUserCommand(user), CancellationToken);

        // Assert
        var dbUser = await Connection.QueryFirstOrDefaultAsync<User>(Sql,
            UserNpgsql.CreateParameterId(user.Id), CancellationToken);
        dbUser.Should().Be(user);
    }
}


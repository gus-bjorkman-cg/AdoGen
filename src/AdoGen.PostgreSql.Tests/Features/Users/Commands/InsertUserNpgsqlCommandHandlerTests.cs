using AdoGen.Sample.Features.Users;
using AdoGen.Sample.Features.Users.Commands;

namespace AdoGen.PostgreSql.Tests.Features.Users.Commands;

public sealed class InsertUserNpgsqlCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly InsertUserCommandHandler _sut = new(testContext.ConnectionString);
    private readonly User _user = UserFaker.Generate();
    private const string SqlGetUser = """SELECT * FROM "public"."Users" WHERE "Id" = @Id""";

    [Fact]
    public async Task InsertUser_ShouldInsertUser()
    {
        // Act
        var insertedId = (await _sut.NpgSql(new InsertUserCommand(_user.Name, _user.Email), CancellationToken)).Id;

        // Assert
        var user = await Connection.QueryFirstOrDefaultAsync<User>(SqlGetUser,
            UserNpgsql.CreateParameterId(insertedId), CancellationToken);

        user.Should().BeEquivalentTo(_user, e => e.Excluding(x => x.Id));
    }

    [Fact]
    public async Task InsertedUser_ShouldBeReturned()
    {
        // Act
        var actual = await _sut.NpgSql(new InsertUserCommand(_user.Name, _user.Email), CancellationToken);

        // Assert
        actual.Should().BeEquivalentTo(_user, e => e.Excluding(x => x.Id));
    }
}


using AdoGen.Sample.Features.Users;
using AdoGen.Sample.Features.Users.Commands;

namespace AdoGen.SqlServer.Tests.Features.Users.Commands;

public sealed class InsertUserCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly InsertUserCommandHandler _sut = new(testContext.ConnectionString);
    private readonly User _user = UserFaker.Generate();
    private const string SqlGetUser = "SELECT * FROM USERS WHERE Id = @Id";

    [Fact]
    public async Task InsertUser_ShouldInsertUser()
    {
        // Act
        var insertedId = (await _sut.SqlServer(new InsertUserCommand(_user.Name, _user.Email), CancellationToken)).Id;
        
        // Assert
        var user = await Connection.QueryFirstOrDefaultAsync<User>(SqlGetUser, 
            UserSql.CreateParameterId(insertedId), CancellationToken);

        user.Should().BeEquivalentTo(_user, e => e.Excluding(x => x.Id));
    }
    
    [Fact]
    public async Task InsertedUser_ShouldBeReturned()
    {
        // Act
        var actual = await _sut.SqlServer(new InsertUserCommand(_user.Name, _user.Email), CancellationToken);
        
        // Assert
        actual.Should().BeEquivalentTo(_user, e => e.Excluding(x => x.Id));
    }
}
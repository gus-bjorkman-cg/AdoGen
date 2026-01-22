using AdoGen.Abstractions;
using AdoGen.Sample.Features.Users;

namespace AdoGen.Tests.Features.Users;

public sealed class InsertUserCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly InsertUserCommandHandler _sut = new(testContext.ConnectionString);
    private readonly User _user = UserFaker.Generate();
    private const string SqlGetUser = "SELECT * FROM USERS WHERE Id = @Id";

    [Fact]
    public async Task InsertUser_ShouldInsertUser()
    {
        // Act
        var insertedId = (await _sut.Handle(new InsertUserCommand(_user.Name, _user.Email), Ct)).Id;
        
        // Assert
        var user = await Connection.QueryFirstOrDefaultAsync<User>(SqlGetUser, 
            UserSql.CreateParameterId(insertedId), Ct);

        user.Should().BeEquivalentTo(_user, e => e.Excluding(x => x.Id));
    }
    
    [Fact]
    public async Task InsertedUser_ShouldBeReturned()
    {
        // Act
        var actual = await _sut.Handle(new InsertUserCommand(_user.Name, _user.Email), Ct);
        
        // Assert
        actual.Should().BeEquivalentTo(_user, e => e.Excluding(x => x.Id));
    }
}
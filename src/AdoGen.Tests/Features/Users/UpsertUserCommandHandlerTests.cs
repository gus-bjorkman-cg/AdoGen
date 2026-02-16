using AdoGen.Abstractions;
using AdoGen.Sample.Features.Users;

namespace AdoGen.Tests.Features.Users;

public sealed class UpsertUserCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly UpsertUserCommandHandler _sut = new(testContext.ConnectionString);
    private readonly User _user = UserFaker.Generate();
    
    private const string Sql = "SELECT * FROM Users WHERE Id = @Id";
    
    [Fact]
    public async Task User_ShouldBeCreated_WhenNotExisting()
    {
        // Act
        await _sut.Handle(new UpsertUserCommand(_user), CancellationToken);
        
        // Assert
        var user = await Connection.QueryFirstOrDefaultAsync<User>(Sql, UserSql.CreateParameterId(_user.Id), CancellationToken);
        user.Should().Be(_user);
    }
    
    [Fact]
    public async Task User_ShouldBeUpdated_WhenExisting()
    {
        // Arrange
        var user = _user with{Id = DefaultUsers[0].Id};
        
        // Act
        await _sut.Handle(new UpsertUserCommand(user), CancellationToken);
        
        // Assert
        var dbUser = await Connection.QueryFirstOrDefaultAsync<User>(Sql, UserSql.CreateParameterId(user.Id), CancellationToken);
        dbUser.Should().Be(user);
    }
}
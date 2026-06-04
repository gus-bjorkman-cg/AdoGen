using AdoGen.Sample.Features.Users;
using AdoGen.Sample.Features.Users.Commands;

namespace AdoGen.SqlServer.Tests.Features.Users.Commands;

public sealed class UpdateUserCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly UpdateUserCommandHandler _sut = new(testContext.ConnectionString);
    private const string Sql = "SELECT * FROM Users WHERE Id = @Id";
    
    [Fact]
    public async Task User_ShouldBeUpdated()
    {
        // Arrange
        var user = DefaultUsers[0] with { Name = "SomeOtherName"};
        
        // Act
        await _sut.SqlServer(new UpdateUserCommand(user), CancellationToken);
        
        // Assert
        var dbUser = await Connection.QueryFirstOrDefaultAsync<User>(Sql, UserSql.CreateParameterId(user.Id), CancellationToken);
        dbUser.Should().Be(user);
    }
}
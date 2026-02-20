using AdoGen.Sample.Features.Users;

namespace AdoGen.Tests.Features.Users;

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
        await _sut.Handle(new UpdateUserCommand(user), CancellationToken);
        
        // Assert
        var dbUser = await Connection.QueryFirstOrDefaultAsync<User>(Sql, UserSql.CreateParameterId(user.Id), CancellationToken);
        dbUser.Should().Be(user);
    }
}
using AdoGen.Sample.Features.Users;

namespace AdoGen.Tests.Features.Users;

public sealed class DeleteUserCommandHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly DeleteUserCommandHandler _sut = new(testContext.ConnectionString);

    private const string SqlSelectById = "SELECT * FROM Users WHERE Id = @id"; 
    
    [Fact]
    public async Task DeleteUserCommand_ShouldDeleteUser()
    {
        // Arrange
        var id = DefaultUsers[0].Id;
        
        // Act
        await _sut.Handle(new DeleteUserCommand(id), CancellationToken);
        
        // Assert
        var user = await Connection.QueryFirstOrDefaultAsync<User>(SqlSelectById, UserSql.CreateParameterId(id), CancellationToken);
        user.Should().BeNull();
    }
}
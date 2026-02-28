using AdoGen.Sample.Features.Users;
using AdoGen.Sample.Features.Users.Commands;

namespace AdoGen.SqlServer.Tests.Features.Users.Commands;

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
        await _sut.SqlServer(new DeleteUserCommand(id), CancellationToken);
        
        // Assert
        var user = await Connection.QueryFirstOrDefaultAsync<User>(SqlSelectById, UserSql.CreateParameterId(id), CancellationToken);
        user.Should().BeNull();
    }
}
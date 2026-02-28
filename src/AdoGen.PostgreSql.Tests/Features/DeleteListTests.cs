using AdoGen.Sample.Features.Users;

namespace AdoGen.PostgreSql.Tests.Features;

public class DeleteListTests(TestContext testContext) : TestBase(testContext)
{
    [Fact]
    public async Task User_ShouldBeDeleted()
    {
        // Arrange
        var ids = DefaultUsers.Select(x => x.Id).ToList();

        // Act
        await Connection.DeleteAsync<User, Guid>(ids, CancellationToken);

        // Assert
        var users = await Connection.QueryAsync<User>("""SELECT * FROM "public"."Users" """, CancellationToken);
        users.Should().BeEmpty();
    }
}


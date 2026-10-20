using AdoGen.Sample.Features.Users;

namespace AdoGen.PostgreSql.Tests.Features;

public sealed class LargeListTests(TestContext testContext) : TestBase(testContext)
{
    private readonly List<User> _users = UserFaker.Generate(2500);

    [Fact]
    public async Task InsertList_ShouldInsertAllRows_WhenListExceedsParameterLimit()
    {
        // Act
        var affected = await Connection.InsertAsync(_users, CancellationToken);

        // Assert
        affected.Should().Be(_users.Count);
        (await GetAllUsers()).Should().HaveCount(_users.Count + DefaultUsers.Count);
    }

    [Fact]
    public async Task DeleteList_ShouldDeleteAllRows_WhenIdCountExceedsParameterLimit()
    {
        // Arrange
        await Connection.InsertAsync(_users, CancellationToken);
        var ids = _users.Select(x => x.Id).Concat(DefaultUsers.Select(x => x.Id)).ToList();

        // Act
        var affected = await Connection.DeleteAsync<User>(ids, CancellationToken);

        // Assert
        affected.Should().Be(ids.Count);
        (await GetAllUsers()).Should().BeEmpty();
    }
}

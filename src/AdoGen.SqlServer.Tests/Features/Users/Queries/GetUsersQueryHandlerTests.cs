using AdoGen.Sample.Features.Users.Queries;

namespace AdoGen.SqlServer.Tests.Features.Users.Queries;

public sealed class GetUsersQueryHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly GetUsersQueryHandler _sut = new(testContext.ConnectionString);

    [Fact]
    public async Task GetUsersQueryHandler_ShouldReturnAllUsers() => 
        (await _sut.SqlServer(GetUsersQuery.Instance, CancellationToken)).Should().BeEquivalentTo(DefaultUsers);
}
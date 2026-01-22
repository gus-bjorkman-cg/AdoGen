using AdoGen.Sample.Features.Users;

namespace AdoGen.Tests.Features.Users;

public sealed class GetUsersQueryHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly GetUsersQueryHandler _sut = new(testContext.ConnectionString);

    [Fact]
    public async Task GetUsersQueryHandler_ShouldReturnAllUsers() => 
        (await _sut.Handle(GetUsersQuery.Instance, Ct)).Should().BeEquivalentTo(DefaultUsers);
}
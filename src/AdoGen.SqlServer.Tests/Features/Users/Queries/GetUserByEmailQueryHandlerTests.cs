using AdoGen.Sample.Features.Users.Queries;

namespace AdoGen.SqlServer.Tests.Features.Users.Queries;

public sealed class GetUserByEmailQueryHandlerTests : TestBase
{
    private readonly GetUserByEmailQueryHandler _sut;

    public GetUserByEmailQueryHandlerTests(TestContext testContext) : base(testContext) => 
        _sut = new GetUserByEmailQueryHandler(ConnectionString);

    [Fact]
    public async Task GetUserByEmailQuery_ShouldReturnCorrectUser() => 
        (await _sut.SqlServer(new GetUserByEmailQuery(DefaultUsers[0].Email), TestContext.CancellationToken))
        .Should()
        .BeEquivalentTo(DefaultUsers[0]);
}
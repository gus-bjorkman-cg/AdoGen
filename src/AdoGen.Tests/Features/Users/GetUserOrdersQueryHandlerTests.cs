using AdoGen.Sample.Features.Users;

namespace AdoGen.Tests.Features.Users;

public sealed class GetUserOrdersQueryHandlerTests : TestBase
{
    private readonly GetUserOrdersQueryHandler _sut;
    private readonly List<GetUserOrdersResponse> _expected = [];

    public GetUserOrdersQueryHandlerTests(TestContext testContext) : base(testContext)
    {
        _sut = new GetUserOrdersQueryHandler(testContext.ConnectionString);
        foreach (var user in DefaultUsers)
        {
            var orders = DefaultOrders.Where(x => x.UserId == user.Id).ToList();
            _expected.Add(new GetUserOrdersResponse(user, orders));
        }
    }

    [Fact]
    public async Task UserOrders_ShouldBeReturned() => 
        (await _sut.Handle(GetUserOrdersQuery.Instance, Ct)) .Should().BeEquivalentTo(_expected);
}
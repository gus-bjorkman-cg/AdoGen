using AdoGen.Sample.Features.Users.Queries;

namespace AdoGen.PostgreSql.Tests.Features.Users.Queries;

public sealed class GetUserOrdersNpgsqlQueryHandlerTests : TestBase
{
    private readonly GetUserOrdersQueryHandler _sut;
    private readonly List<GetUserOrdersResponse> _expected = [];

    public GetUserOrdersNpgsqlQueryHandlerTests(TestContext testContext) : base(testContext)
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
        (await _sut.NpgSql(GetUserOrdersQuery.Instance, CancellationToken)).Should().BeEquivalentTo(_expected);
}


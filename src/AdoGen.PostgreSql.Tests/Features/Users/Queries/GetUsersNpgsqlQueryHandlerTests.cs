using AdoGen.Sample.Features.Users.Queries;

namespace AdoGen.PostgreSql.Tests.Features.Users.Queries;

public sealed class GetUsersNpgsqlQueryHandlerTests(TestContext testContext) : TestBase(testContext)
{
    private readonly GetUsersQueryHandler _sut = new(testContext.ConnectionString);

    [Fact]
    public async Task GetUsersQueryHandler_ShouldReturnAllUsers() =>
        (await _sut.NpgSql(GetUsersQuery.Instance, CancellationToken)).Should().BeEquivalentTo(DefaultUsers);
}


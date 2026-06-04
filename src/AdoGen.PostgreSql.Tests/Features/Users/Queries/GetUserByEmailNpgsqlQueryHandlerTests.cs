using AdoGen.Sample.Features.Users.Queries;

namespace AdoGen.PostgreSql.Tests.Features.Users.Queries;

public sealed class GetUserByEmailNpgsqlQueryHandlerTests : TestBase
{
    private readonly GetUserByEmailQueryHandler _sut;

    public GetUserByEmailNpgsqlQueryHandlerTests(TestContext testContext) : base(testContext) =>
        _sut = new GetUserByEmailQueryHandler(ConnectionString);

    [Fact]
    public async Task GetUserByEmailQuery_ShouldReturnCorrectUser() =>
        (await _sut.NpgSql(new GetUserByEmailQuery(DefaultUsers[0].Email), TestContext.CancellationToken))
        .Should()
        .BeEquivalentTo(DefaultUsers[0]);
}


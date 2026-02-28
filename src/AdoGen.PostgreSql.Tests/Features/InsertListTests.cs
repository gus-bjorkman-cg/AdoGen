using AdoGen.Sample.Features.Users;

namespace AdoGen.PostgreSql.Tests.Features;

public sealed class InsertListTests(TestContext testContext) : TestBase(testContext)
{
    private readonly List<User> _users = UserFaker.Generate(2);

    [Fact]
    public async Task User_ShouldBeInserted()
    {
        // Act
        await Connection.InsertAsync(_users, CancellationToken);

        // Assert
        (await GetUsers()).Should().BeEquivalentTo(_users);
    }

    [Fact]
    public async Task Insert_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);

        // Act
        await Connection.InsertAsync(_users, CancellationToken, transaction);
        await transaction.RollbackAsync(CancellationToken);

        // Assert
        (await GetUsers()).Should().BeEmpty();
    }

    private async ValueTask<List<User>> GetUsers() =>
        await Connection.QueryAsync<User>(
            """SELECT * FROM "public"."Users" WHERE "Id" IN (@Id, @Id2)""",
            [new NpgsqlParameter<Guid>("Id", _users[0].Id), new NpgsqlParameter<Guid>("Id2", _users[1].Id)],
            CancellationToken);
}


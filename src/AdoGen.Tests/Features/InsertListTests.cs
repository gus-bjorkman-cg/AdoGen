using AdoGen.Sample.Features.Users;

namespace AdoGen.Tests.Features;

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
        await using var transaction = Connection.BeginTransaction();
        
        // Act
        await Connection.InsertAsync(_users, CancellationToken, transaction);
        transaction.Rollback();

        // Assert
        (await GetUsers()).Should().BeEmpty();
    }

    [Fact]
    public async Task Insert_ShouldRespectCommandTimeout()
    {
        // Arrange
        await using var transaction = await LockTable("Users");
        
        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await Connection.InsertAsync(_users, CancellationToken, commandTimeout: 1);
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        transaction.Rollback();
    }
    
    private async ValueTask<List<User>> GetUsers() =>
        await Connection.QueryAsync<User>("SELECT * FROM Users WHERE Id IN (@Id, @Id2)",
            [UserSql.CreateParameterId(_users[0].Id), UserSql.CreateParameterId(_users[1].Id, "Id2")]
            , CancellationToken);
}
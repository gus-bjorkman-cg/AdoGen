using AdoGen.Abstractions;
using AdoGen.Sample.Features.Users;
using Microsoft.Data.SqlClient;

namespace AdoGen.Tests.Features;

public sealed class Insert(TestContext testContext) : TestBase(testContext)
{
    private readonly User _user = UserFaker.Generate();
    
    [Fact]
    public async Task User_ShouldBeInserted()
    {
        // Act
        await Connection.InsertAsync(_user, Ct);
        
        // Assert
        (await GetUser()).Should().BeEquivalentTo(_user);
    }

    [Fact]
    public async Task Insert_ShouldRespectDbTransaction()
    {
        // Arrange
        await using var transaction = Connection.BeginTransaction();
        
        // Act
        await Connection.InsertAsync(_user, Ct, transaction);
        transaction.Rollback();

        // Assert
        (await GetUser()).Should().BeNull();
    }

    [Fact]
    public async Task Insert_ShouldRespectCommandTimeout()
    {
        // Arrange
        await using var transaction = await LockUserTable();
        
        // Act
        var act = async () =>
        {
            await using var connectionB = new SqlConnection(ConnectionString);
            await Connection.InsertAsync(_user, Ct, commandTimeout: 1);
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        transaction.Rollback();
    }
    
    private async ValueTask<User?> GetUser() =>
        await Connection.QueryFirstOrDefaultAsync<User>("SELECT TOP(1) * FROM Users WHERE Id = @Id",
            UserSql.CreateParameterId(_user.Id), Ct);
}
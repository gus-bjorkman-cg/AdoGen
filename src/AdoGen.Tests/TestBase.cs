using AdoGen.Abstractions;
using AdoGen.Sample.Features.Orders;
using AdoGen.Sample.Features.Users;
using Bogus;
using Bogus.Extensions;
using Microsoft.Data.SqlClient;

namespace AdoGen.Tests;

[Collection(TestCollection.Name)]
public abstract class TestBase : IAsyncLifetime
{
    protected string ConnectionString { get; }
    protected List<User> DefaultUsers { get; }
    protected List<Order> DefaultOrders { get; }
    protected readonly SqlConnection Connection;
    protected static CancellationToken CancellationToken => TestContext.CancellationToken;

    protected static readonly Faker<User> UserFaker = new Faker<User>()
        .RuleFor(x => x.Id, Guid.CreateVersion7)
        .RuleFor(x => x.Name, y => y.Person.FullName.ClampLength(1, 20))
        .RuleFor(x => x.Email, y => y.Person.Email.ClampLength(1, 50))
        .WithDefaultConstructor();

    protected TestBase(TestContext testContext)
    {
        ConnectionString = testContext.ConnectionString;
        Connection = new SqlConnection(ConnectionString);
        DefaultUsers = UserFaker.Generate(10);
        DefaultOrders = new Faker<Order>()
            .CustomInstantiator(x => new Order(Guid.CreateVersion7(), x.Commerce.Product(), x.PickRandom(DefaultUsers).Id))
            .Generate(20);
    }

    protected async ValueTask<SqlTransaction> LockUserTable()
    {
        var transaction = Connection.BeginTransaction();
        await using var cmd = new SqlCommand("SELECT * FROM Users WITH (TABLOCKX)", Connection, transaction);
        await cmd.ExecuteNonQueryAsync(CancellationToken);
        
        return transaction;
    }

    private const string GetUserSql = "SELECT TOP(1) * FROM Users WHERE Id = @Id"; 
    protected async ValueTask<User?> GetUser(Guid id) =>
        await Connection.QueryFirstOrDefaultAsync<User>(GetUserSql, UserSql.CreateParameterId(id), CancellationToken);

    protected virtual ValueTask InitializeAsync() => ValueTask.CompletedTask;
    protected virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
    
    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await Connection.InsertAsync(DefaultUsers, CancellationToken);
        await Connection.InsertAsync(DefaultOrders, CancellationToken);
        await InitializeAsync();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await Connection.TruncateAsync<User>(CancellationToken);
        await Connection.TruncateAsync<Order>(CancellationToken);
        await DisposeAsync();
        Connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
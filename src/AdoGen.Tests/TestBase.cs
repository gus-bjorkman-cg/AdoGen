using AdoGen.Abstractions;
using AdoGen.Sample.Features.Orders;
using AdoGen.Sample.Features.Users;
using Bogus;
using Microsoft.Data.SqlClient;

namespace AdoGen.Tests;

[Collection(TestCollection.Name)]
public abstract class TestBase : IAsyncLifetime
{
    protected string ConnectionString { get; }
    protected List<User> DefaultUsers { get; }
    protected List<Order> DefaultOrders { get; }
    protected readonly SqlConnection Connection;
    protected static CancellationToken Ct => TestContext.CancellationToken;
    
    protected static readonly Faker<User> UserFaker = new Faker<User>()
        .CustomInstantiator(x => new User(Guid.CreateVersion7(), x.Person.FullName, x.Person.Email));

    protected TestBase(TestContext testContext)
    {
        ConnectionString = testContext.ConnectionString;
        Connection = new SqlConnection(ConnectionString);
        DefaultUsers = UserFaker.Generate(10);
        DefaultOrders = new Faker<Order>()
            .CustomInstantiator(x => new Order(Guid.CreateVersion7(), x.Commerce.Product(), x.PickRandom(DefaultUsers).Id))
            .Generate(20);
    }

    protected virtual ValueTask InitializeAsync() => ValueTask.CompletedTask;
    protected virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
    
    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await Connection.Insert(DefaultUsers, Ct);
        await Connection.Insert(DefaultOrders, Ct);
        await InitializeAsync();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await Connection.Truncate<User>(Ct);
        await Connection.Truncate<Order>(Ct);
        await DisposeAsync();
        Connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
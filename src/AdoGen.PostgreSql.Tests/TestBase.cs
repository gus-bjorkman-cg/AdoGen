using AdoGen.Sample.Features.Orders;
using AdoGen.Sample.Features.Users;
using Bogus;
using Bogus.Extensions;

namespace AdoGen.PostgreSql.Tests;

[Collection(TestCollection.Name)]
public abstract class TestBase : IAsyncLifetime
{
    protected string ConnectionString { get; }
    protected List<User> DefaultUsers { get; }
    protected List<Order> DefaultOrders { get; }

    protected readonly NpgsqlConnection Connection;

    protected static CancellationToken CancellationToken => TestContext.CancellationToken;

    protected static readonly Faker<User> UserFaker = new Faker<User>()
        .RuleFor(x => x.Id, Guid.CreateVersion7)
        .RuleFor(x => x.Name, y => y.Person.FullName.ClampLength(1, 20))
        .RuleFor(x => x.Email, y => y.Person.Email.ClampLength(1, 50))
        .WithDefaultConstructor();

    protected TestBase(TestContext testContext)
    {
        ConnectionString = testContext.ConnectionString;
        Connection = new NpgsqlConnection(ConnectionString);

        DefaultUsers = UserFaker.Generate(10);
        DefaultOrders = new Faker<Order>()
            .CustomInstantiator(x => new Order(Guid.CreateVersion7(), x.Commerce.Product(), x.PickRandom(DefaultUsers).Id))
            .Generate(20);
    }

    private const string GetUserSql = """SELECT * FROM "public"."Users" WHERE "Id" = @Id""";

    protected async ValueTask<User?> GetUser(Guid id) =>
        await Connection.QueryFirstOrDefaultAsync<User>(GetUserSql,
            new NpgsqlParameter<Guid>("Id", id),
            CancellationToken);

    protected async ValueTask<List<User>> GetAllUsers() =>
        await Connection.QueryAsync<User>("""SELECT * FROM "public"."Users" """, CancellationToken);

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


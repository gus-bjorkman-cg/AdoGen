using AdoGen.Sample.Features.Audit;
using AdoGen.Sample.Features.Orders;
using AdoGen.Sample.Features.TestTypes;
using AdoGen.Sample.Features.Users;
using Testcontainers.PostgreSql;

namespace AdoGen.PostgreSql.Tests;

public sealed class TestContext : IAsyncLifetime
{
    private PostgreSqlContainer _pgContainer = null!;

    public string ConnectionString { get; private set; } = "";
    public static CancellationToken CancellationToken => Xunit.TestContext.Current.CancellationToken;

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        _pgContainer = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _pgContainer.StartAsync(CancellationToken);

        ConnectionString = _pgContainer.GetConnectionString();

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken);

        // Schema "log" is used by AuditEvent in the sample.
        await using (var schema = connection.CreateCommand("CREATE SCHEMA log;"))
        {
            await schema.ExecuteNonQueryAsync(CancellationToken);
        }

        await connection.CreateTableAsync<User>(CancellationToken);
        await connection.CreateTableAsync<Order>(CancellationToken);
        await connection.CreateTableAsync<TestType>(CancellationToken);
        await connection.CreateTableAsync<AuditEvent>(CancellationToken);
    }

    public async ValueTask DisposeAsync() => await _pgContainer.DisposeAsync();
}


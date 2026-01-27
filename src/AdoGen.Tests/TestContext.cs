using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using AdoGen.Abstractions;
using AdoGen.Sample.Features.Orders;
using User = AdoGen.Sample.Features.Users.User;

namespace AdoGen.Tests;

public sealed class TestContext : IAsyncLifetime
{
    private MsSqlContainer _msSqlContainer = null!;
    public string ConnectionString { get; private set; } = "";
    public static CancellationToken CancellationToken => Xunit.TestContext.Current.CancellationToken;

    private const string SqlCreateDb = "CREATE DATABASE [TestDb];";
    
    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        _msSqlContainer = new MsSqlBuilder().Build();
        await _msSqlContainer.StartAsync(CancellationToken);
        ConnectionString = _msSqlContainer.GetConnectionString();
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await using var command = connection.CreateCommand(SqlCreateDb);
        await command.ExecuteNonQueryAsync(CancellationToken);
        await connection.CreateTableAsync<User>(CancellationToken);
        await connection.CreateTableAsync<Order>(CancellationToken);
    }

    public async ValueTask DisposeAsync() => await _msSqlContainer.DisposeAsync();
}
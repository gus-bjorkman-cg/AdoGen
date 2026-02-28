using AdoGen.Sample.Features.Audit;
using Testcontainers.MsSql;
using AdoGen.Sample.Features.Orders;
using AdoGen.Sample.Features.TestTypes;
using AdoGen.Sample.Features.Users;

namespace AdoGen.SqlServer.Tests;

public sealed class TestContext : IAsyncLifetime
{
    private MsSqlContainer _msSqlContainer = null!;
    public string ConnectionString { get; private set; } = "";
    public static CancellationToken CancellationToken => Xunit.TestContext.Current.CancellationToken;

    private const string SqlCreateDb = "CREATE DATABASE [TestDb];";
    private const string SqlCreateLogSchema = "CREATE SCHEMA [log] AUTHORIZATION [dbo];";
    
    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        _msSqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();
        await _msSqlContainer.StartAsync(CancellationToken);
        ConnectionString = _msSqlContainer.GetConnectionString();
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken);
        
        await using var command = connection.CreateCommand(SqlCreateDb);
        await command.ExecuteNonQueryAsync(CancellationToken);
        await using var schemaCommand = connection.CreateCommand(SqlCreateLogSchema);
        await schemaCommand.ExecuteNonQueryAsync(CancellationToken);
        
        await connection.CreateTableAsync<User>(CancellationToken);
        await connection.CreateTableAsync<Order>(CancellationToken);
        await connection.CreateTableAsync<TestType>(CancellationToken);
        await connection.CreateTableAsync<AuditEvent>(CancellationToken);
    }

    public async ValueTask DisposeAsync() => await _msSqlContainer.DisposeAsync();
}
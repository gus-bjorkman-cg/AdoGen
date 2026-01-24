using Microsoft.Data.SqlClient;
using AdoGen.Abstractions;
using Testcontainers.MsSql;

namespace AdoGen.Benchmarks;

public static class TestContext
{
    private static MsSqlContainer? _msSqlContainer;
    public static string ConnectionString { get; private set; } = "";
    private static readonly CancellationTokenSource CancellationTokenSource = new();
    public static CancellationToken CancellationToken => CancellationTokenSource.Token;
    
    private const string SqlCreateDb = "CREATE DATABASE [TestDb]";
    
    public static async Task InitializeAsync()
    {
        _msSqlContainer = new MsSqlBuilder().Build();
        await _msSqlContainer.StartAsync(CancellationToken);
        ConnectionString = _msSqlContainer.GetConnectionString();
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await using var command = connection.CreateCommand(SqlCreateDb);
        await command.ExecuteNonQueryAsync(CancellationToken);
        
        ConnectionString = new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "TestDb"}.ConnectionString;
        
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync(CancellationToken);
        await using var createTableCommand = conn.CreateCommand(CreateUsersSql);
        await createTableCommand.ExecuteNonQueryAsync(CancellationToken);
        await using var seedCommand = conn.CreateCommand(SeedUsersSql);
        await seedCommand.ExecuteNonQueryAsync(CancellationToken);
    }

    private const string CreateUsersSql =
        """
        CREATE TABLE dbo.Users (
        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() CONSTRAINT PK_Users PRIMARY KEY,
        Name VARCHAR(20) NOT NULL,
        Email VARCHAR(50) NOT NULL UNIQUE
        );

        CREATE NONCLUSTERED INDEX IX_Users_Name ON dbo.Users (Name);
        """;

        private const string SeedUsersSql =
        """
        SET NoCount ON;
        DECLARE @index INT = 0;
        
        WHILE @index <= 101
        BEGIN
            INSERT INTO Users (Id, Name, Email) VALUES (NEWID(), CAST(@index AS VARCHAR), CAST(@index AS VARCHAR));
            SET @index = @index + 1;
        END
        """;
    
    public static async Task Dispose()
    {
        if (_msSqlContainer is not null) await _msSqlContainer.DisposeAsync();
        CancellationTokenSource.Dispose();
    }
}
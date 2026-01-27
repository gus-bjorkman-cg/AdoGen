using Microsoft.Data.SqlClient;
using AdoGen.Abstractions;
using BenchmarkDotNet.Attributes;
using Testcontainers.MsSql;

namespace AdoGen.Benchmarks;

public abstract class TestBase
{
    private static MsSqlContainer? _msSqlContainer;
    private static readonly CancellationTokenSource CancellationTokenSource = new();
    protected static CancellationToken CancellationToken => CancellationTokenSource.Token;
    
    protected static string ConnectionString { get; private set; } = "";

    protected static int Index { get; set => field = field == 1000 ? 0 : value; }

    private const string SqlCreateDb = "CREATE DATABASE [TestDb]";
    protected const string SqlGetOne = "SELECT TOP(1) * FROM Users WHERE Name = @Name";
    protected const string SqlGetTen = "SELECT TOP(10) * FROM Users";
    
    [GlobalSetup]
    public async Task InitializeAsync()
    {
        _msSqlContainer = new MsSqlBuilder().Build();
        await _msSqlContainer.StartAsync(CancellationToken);
        ConnectionString = _msSqlContainer.GetConnectionString();
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await using var command = connection.CreateCommand(SqlCreateDb);
        await command.ExecuteNonQueryAsync(CancellationToken);
        
        ConnectionString = new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "TestDb" }.ConnectionString;
        
        var sqlConnection = new SqlConnection(ConnectionString);
        await sqlConnection.OpenAsync(CancellationToken);
        await using var createTableCommand = sqlConnection.CreateCommand(CreateUsersSql);
        await createTableCommand.ExecuteNonQueryAsync(CancellationToken);
        await using var seedCommand = sqlConnection.CreateCommand(SeedUsersSql);
        await seedCommand.ExecuteNonQueryAsync(CancellationToken);
        await Initialize();
    }

    protected virtual ValueTask Initialize() => ValueTask.CompletedTask;

    private const string CreateUsersSql =
        """
        CREATE TABLE dbo.Users (
        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() CONSTRAINT PK_Users PRIMARY KEY,
        Name VARCHAR(20) NOT NULL,
        Email VARCHAR(50) NOT NULL
        );

        CREATE NONCLUSTERED INDEX IX_Users_Name ON dbo.Users (Name);
        """;

        private const string SeedUsersSql =
        """
        SET NoCount ON;
        DECLARE @index INT = 0;
        
        WHILE @index < 1001
        BEGIN
            INSERT INTO Users (Id, Name, Email) VALUES (NEWID(), CAST(@index AS VARCHAR), CAST(@index AS VARCHAR));
            SET @index = @index + 1;
        END
        """;
    
    [GlobalCleanup]
    public async Task DisposeAsync()
    {
        await Dispose();
        if (_msSqlContainer is not null) await _msSqlContainer.DisposeAsync();
        CancellationTokenSource.Dispose();
    }
    
    protected virtual ValueTask Dispose() => ValueTask.CompletedTask;
}
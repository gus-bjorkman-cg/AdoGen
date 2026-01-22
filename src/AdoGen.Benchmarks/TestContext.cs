using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using AdoGen.Abstractions;
using Testcontainers.MsSql;
using User = AdoGen.Sample.Features.Users.User;

namespace AdoGen.Benchmarks;

public static class TestContext
{
    private static MsSqlContainer? _msSqlContainer;
    public static string ConnectionString { get; private set; } = "";
    private static readonly CancellationTokenSource CancellationTokenSource = new();
    public static CancellationToken CancellationToken => CancellationTokenSource.Token;
    
    private const string SqlCreateDb =
        """
        CREATE DATABASE [TestDb];

        CREATE TABLE dbo.Users (
            Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() CONSTRAINT PK_Users PRIMARY KEY,
            Name VARCHAR(20) NOT NULL,
            Email VARCHAR(50) NOT NULL UNIQUE
        );
        
        CREATE NONCLUSTERED INDEX IX_Users_Name
            ON dbo.Users (Name);
        """;
    
    public static async Task InitializeAsync()
    {
        _msSqlContainer = new MsSqlBuilder().Build();
        await _msSqlContainer.StartAsync(CancellationToken);
        ConnectionString = _msSqlContainer.GetConnectionString();
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await using var command = connection.CreateCommand(SqlCreateDb);
        await command.ExecuteNonQueryAsync(CancellationToken);
        
        var users = new List<User>();

        for (var i = 0; i < 100; i++)
        {
            users.Add(new User(Guid.CreateVersion7(), i.ToString(), i.ToString()));
        }
        
        await InsertUsers(users);
    }
    
    private const string InsertUserSql = "INSERT INTO Users (Id, Name, Email) VALUES (@Id, @Name, @Email)";
    
    private static async ValueTask InsertUsers(params List<User> users)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await using var command = connection.CreateCommand();
        var sqlBuilder = new StringBuilder(InsertUserSql);
        
        for (var i = 0; i < users.Count; i++)
        {
            var paramIndex = i == 0 ? "" : i.ToString();
            
            command.CreateParameter($"@Id{paramIndex}", users[i].Id, SqlDbType.UniqueIdentifier);
            command.CreateParameter($"@Name{paramIndex}", users[i].Name, SqlDbType.VarChar, 20);
            command.CreateParameter($"@Email{paramIndex}", users[i].Email, SqlDbType.VarChar, 50);

            if (i == 0) continue;

            sqlBuilder.Append(',');
            sqlBuilder.AppendLine($"(@Id{paramIndex}, @Name{paramIndex}, @Email{paramIndex})");
        }
        
        command.CommandText = sqlBuilder.ToString();
        
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    public static async Task Dispose()
    {
        if (_msSqlContainer is not null) await _msSqlContainer.DisposeAsync();
        CancellationTokenSource.Dispose();
    }
}
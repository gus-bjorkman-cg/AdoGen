using AdoGen.PostgreSql;
using AdoGen.Sample.Features.Users;
using BenchmarkDotNet.Attributes;
using Bogus;
using Bogus.Extensions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace AdoGen.Benchmarks;

public abstract class PgTestBase
{
    private static PostgreSqlContainer? _pgContainer;
    private string _connectionString = "";

    private static readonly CancellationTokenSource CancellationTokenSource = new();
    protected static CancellationToken CancellationToken => CancellationTokenSource.Token;

    protected NpgsqlConnection Connection { get; private set; } = null!;
    protected PgTestDbContext DbContext { get; private set; } = null!;

    protected const string SqlGetOne = """SELECT * FROM "public"."Users" WHERE "Name" = @Name LIMIT 1""";
    protected const string SqlGetTen = """SELECT * FROM "public"."Users" ORDER BY "Id" OFFSET @offset LIMIT 10""";

    protected static readonly Faker<User> UserFaker = new Faker<User>()
        .RuleFor(x => x.Id, Guid.CreateVersion7)
        .RuleFor(x => x.Name, y => y.Person.FullName.ClampLength(1, 20))
        .RuleFor(x => x.Email, y => y.Person.Email.ClampLength(1, 50))
        .WithDefaultConstructor();

    [GlobalSetup]
    public async Task InitializeAsync()
    {
        _pgContainer = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _pgContainer.StartAsync(CancellationToken);
        _connectionString = _pgContainer.GetConnectionString();

        await using var setupConnection = new NpgsqlConnection(_connectionString);
        await setupConnection.OpenAsync(CancellationToken);
        await setupConnection.CreateTableAsync<User>(CancellationToken);
        await using var seedCommand = setupConnection.CreateCommand(SeedUsersSql);
        await seedCommand.ExecuteNonQueryAsync(CancellationToken);

        Connection = new NpgsqlConnection(_connectionString);
        await Connection.OpenAsync(CancellationToken);

        var dbContextOptions = new DbContextOptionsBuilder<PgTestDbContext>()
            .UseNpgsql(Connection)
            .Options;
        DbContext = new PgTestDbContext(dbContextOptions);
        await Initialize();
    }

    protected virtual ValueTask Initialize() => ValueTask.CompletedTask;

    private const string SeedUsersSql =
        """
        DO $$
        DECLARE i INT := 0;
        BEGIN
            WHILE i < 1001 LOOP
                INSERT INTO "public"."Users" ("Id", "Name", "Email")
                VALUES (gen_random_uuid(), i::TEXT, i::TEXT);
                i := i + 1;
            END LOOP;
        END;
        $$;
        """;

    [GlobalCleanup]
    public async Task DisposeAsync()
    {
        await Dispose();
        await Connection.DisposeAsync();
        await DbContext.DisposeAsync();
        if (_pgContainer is not null) await _pgContainer.DisposeAsync();
        CancellationTokenSource.Dispose();
    }

    protected virtual ValueTask Dispose() => ValueTask.CompletedTask;
}


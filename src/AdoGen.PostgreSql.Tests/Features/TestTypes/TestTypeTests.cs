using AdoGen.Sample.Features.TestTypes;
using Bogus;

namespace AdoGen.PostgreSql.Tests.Features.TestTypes;

public sealed class TestTypeTests(TestContext testContext) : TestBase(testContext)
{
    private static readonly Faker<TestType> Faker = Fakers.TestTypeFaker;

    private List<TestType> _toInsert = [];
    private readonly List<TestType> _toUpdate = [];
    private List<TestType> _toDelete = [];

    protected override async ValueTask InitializeAsync()
    {
        var testTypes = Faker.Generate(40);
        for (var i = 0; i < testTypes.Count; i++)
        {
            var index = i + 1;
            testTypes[i] = testTypes[i] with { Int = index, Decimal = index };
        }

        _toInsert = testTypes.Take(10).ToList();
        var toUpdate = testTypes.Skip(10).Take(10).ToList();
        _toDelete = testTypes.Skip(20).Take(10).ToList();

        for (var i = 0; i < toUpdate.Count; i++)
        {
            _toUpdate.Add(testTypes[i + 30] with { Int = toUpdate[i].Int, Decimal = toUpdate[i].Decimal });
        }

        await Connection.InsertAsync(_toDelete.Concat(toUpdate).ToList(), CancellationToken);
    }

    protected override async ValueTask DisposeAsync() => await Connection.TruncateAsync<TestType>(CancellationToken);

    private async ValueTask<List<TestType>> GetAll() =>
        await Connection.QueryAsync<TestType>("""SELECT * FROM "public"."TestTypes" """, CancellationToken);

    [Fact]
    public async Task InsertReadOnlyField_ShouldNotBeWritten()
    {
        // Arrange
        var item = _toInsert.First();

        // Act
        await Connection.InsertAsync(item, CancellationToken);

        // Assert
        var actual = await Connection.QueryFirstOrDefaultAsync<TestType>(
            """SELECT * FROM "public"."TestTypes" WHERE "Int" = @Int LIMIT 1""",
            TestTypeNpgsql.CreateParameterInt(item.Int), CancellationToken);

        actual!.CreatedAt.UtcDateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task UpdateReadOnlyField_ShouldNotBeOverwritten()
    {
        // Arrange
        var item = _toUpdate.First();

        // Act
        await Connection.UpdateAsync(item, CancellationToken);

        // Assert
        var actual = await Connection.QueryFirstOrDefaultAsync<TestType>(
            """SELECT * FROM "public"."TestTypes" WHERE "Int" = @Int LIMIT 1""",
            TestTypeNpgsql.CreateParameterInt(item.Int), CancellationToken);

        actual!.CreatedAt.UtcDateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task UpsertReadOnlyField_ShouldNotBeWritten_WhenInsert()
    {
        // Arrange
        var item = _toInsert.First();

        // Act
        await Connection.UpsertAsync(item, CancellationToken);

        // Assert
        var actual = await Connection.QueryFirstOrDefaultAsync<TestType>(
            """SELECT * FROM "public"."TestTypes" WHERE "Int" = @Int LIMIT 1""",
            TestTypeNpgsql.CreateParameterInt(item.Int), CancellationToken);

        actual!.CreatedAt.UtcDateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task UpsertReadOnlyField_ShouldNotBeWritten_WhenUpdate()
    {
        // Arrange
        var item = _toUpdate.First();
        var timestamp = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await Connection.ExecuteAsync("""UPDATE "public"."TestTypes" SET "CreatedAt" = '2000-01-01T00:00:00Z'""", CancellationToken);

        // Act
        await Connection.UpsertAsync(item, CancellationToken);

        // Assert
        var afterUpdate = await Connection.QueryFirstOrDefaultAsync<TestType>(
            """SELECT * FROM "public"."TestTypes" WHERE "Int" = @Int LIMIT 1""",
            TestTypeNpgsql.CreateParameterInt(item.Int), CancellationToken);
        
        afterUpdate!.CreatedAt.Should().Be(timestamp);
    }

    [Fact]
    public async Task BulkInsertReadOnlyField_ShouldNotBeWritten()
    {
        // Arrange
        var bulk = new TestTypeNpgsqlBulk(10);
        bulk.AddRange(_toInsert);
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);

        // Act
        await bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);

        // Assert
        var inserted = await GetAll();
        
        inserted.Should().AllSatisfy(x =>
            x.CreatedAt.UtcDateTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public async Task BulkUpdateReadOnlyField_ShouldNotBeOverwritten()
    {
        // Arrange
        var existing = await GetAll();
        var timestamp = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await Connection.ExecuteAsync("""UPDATE "public"."TestTypes" SET "CreatedAt" = '2000-01-01T00:00:00Z'""", CancellationToken);

        var bulk = new TestTypeNpgsqlBulk(existing.Count);
        bulk.UpdateRange(existing);
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);

        // Act
        await bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);

        // Assert
        var afterUpdate = await GetAll();
        afterUpdate.Should().AllSatisfy(x => x.CreatedAt.Should().Be(timestamp));
    }

    [Fact]
    public async Task BulkMixed_ShouldPerformAllOperations()
    {
        // Arrange
        var bulk = new TestTypeNpgsqlBulk(30);
        bulk.AddRange(_toInsert);
        bulk.UpdateRange(_toUpdate);
        bulk.RemoveRange(_toDelete);
        await using var transaction = await Connection.BeginTransactionAsync(CancellationToken);

        // Act
        await bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);

        // Assert
        (await GetAll()).Should().BeEquivalentTo(_toInsert.Concat(_toUpdate),
            options => options.Excluding(x => x.CreatedAt));
    }
}

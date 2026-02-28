using AdoGen.Sample.Features.TestTypes;
using Bogus;
using Bogus.Extensions;

namespace AdoGen.SqlServer.Tests.Features.TestTypes;

public sealed class TestTypeTests(TestContext testContext) : TestBase(testContext)
{
    private static DateTime RoundToSqlServerDateTime(DateTime value) => 
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second);

    private static readonly Faker<TestType> Faker = new Faker<TestType>()
        .StrictMode(true)
        .RuleFor(x => x.Int, _ => 0)
        .RuleFor(x => x.NullableInt, f => f.Random.Bool() ? f.Random.Int() : null)
        .RuleFor(x => x.Decimal, _ => 0)
        .RuleFor(x => x.NullableDecimal, f => f.Random.Bool() ? Math.Round(f.Random.Decimal(0m, 9.99999m), 2, MidpointRounding.AwayFromZero) : null)
        .RuleFor(x => x.NullableGuid, f => f.Random.Bool() ? f.Random.Guid() : null)
        .RuleFor(x => x.NullableStringVarchar, f => f.Random.Bool() ? f.Lorem.Text().ClampLength(1, 100) : null)
        .RuleFor(x => x.NullableStringNVarchar, f => f.Random.Bool() ? f.Lorem.Text().ClampLength(1, 100) : null)
        .RuleFor(x => x.StringVarcharRuledNull, f => f.Lorem.Text().ClampLength(1, 100))
        .RuleFor(x => x.CharString, f => f.Random.String2(10))
        .RuleFor(x => x.NCharString, f => f.Random.String2(15))
        .RuleFor(x => x.Float, f => f.Random.Float())
        .RuleFor(x => x.NullableFloat, f => f.Random.Bool() ? f.Random.Float() : null)
        .RuleFor(x => x.DateTime, f => f.Date.Recent())
        .RuleFor(x => x.NullableDateTime, f => RoundToSqlServerDateTime(f.Date.Recent()))
        .RuleFor(x => x.Double, f => f.Random.Double())
        .RuleFor(x => x.NullableDouble, f => f.Random.Bool() ? f.Random.Double() : null)
        .RuleFor(x => x.Char, f => f.Random.Char('a', 'z'))
        .RuleFor(x => x.NChar, f => f.Random.Char('A', 'Z'))
        .RuleFor(x => x.NullableChar, f => f.Random.Bool() ? f.Random.Char('0', '9') : null)
        .RuleFor(x => x.NullableBytes, f => f.Random.Bool() ? f.Random.Bytes(f.Random.Int(0, 200)) : null)
        .RuleFor(x => x.Bytes, f => f.Random.Bytes(5))
        .RuleFor(x => x.Fruits, f => f.PickRandom<Fruits>())
        .RuleFor(x => x.Flags, f =>
        {
            var value = Flags.None;
            if (f.Random.Bool()) value |= Flags.Flag1;
            if (f.Random.Bool()) value |= Flags.Flag2;
            if (f.Random.Bool()) value |= Flags.Flag3;
            return value;
        })
        .RuleFor(x => x.ByteEnum, f => f.PickRandom<ByteEnum>())
        .RuleFor(x => x.ShortEnum, f => f.PickRandom<ShortEnum>())
        .RuleFor(x => x.IntEnum, f => f.PickRandom<IntEnum>())
        .RuleFor(x => x.LongEnum, f => f.PickRandom<LongEnum>())
        .WithDefaultConstructor();

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

    private async ValueTask<List<TestType>> GetAll() => await Connection.QueryAsync<TestType>("SELECT * FROM TestTypes", CancellationToken);
    
    [Fact]
    public async Task BulkMixed_ShouldPerformAllOperations()
    {
        // Arrange
        var bulk = new TestTypeBulk(30);
        bulk.AddRange(_toInsert);
        bulk.UpdateRange(_toUpdate);
        bulk.RemoveRange(_toDelete);
        await using var transaction = (SqlTransaction)await Connection.BeginTransactionAsync(CancellationToken);
        
        // Act
        await bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);
        
        // Assert
        (await GetAll()).Should().BeEquivalentTo(_toInsert.Concat(_toUpdate));
    }
}
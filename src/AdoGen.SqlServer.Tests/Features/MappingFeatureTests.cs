using AdoGen.Sample.Features.Gauges;
using static AdoGen.Sample.Features.Gauges.Telemetry;

namespace AdoGen.SqlServer.Tests.Features;

// FEATURES.md section B: nested DTOs, unsigned/char defaults, enum AsString, Ignore and struct mappers.
public sealed class MappingFeatureTests(TestContext testContext) : TestBase(testContext)
{
    private const string SelectAll = """SELECT * FROM [dbo].[Gauges]""";

    private static Gauge CreateMax() => new(
        Guid.CreateVersion7(), sbyte.MinValue, ushort.MaxValue, uint.MaxValue, ulong.MaxValue, ulong.MaxValue,
        'Z', GaugeState.Faulted, GaugeState.Running) { Cache = "not persisted" };

    private static Gauge CreateNulls() => new(
        Guid.CreateVersion7(), 1, 2, 3, 4, null, 'a', GaugeState.Idle, null);

    protected override async ValueTask DisposeAsync() => await Connection.TruncateAsync<Gauge>(CancellationToken);

    [Fact]
    public async Task Insert_ShouldRoundTripUnsignedCharAndEnumValues_WhenAtLimits()
    {
        // Arrange
        var expected = CreateMax();

        // Act
        await Connection.InsertAsync(expected, CancellationToken);
        var actual = await Connection.QueryFirstOrDefaultAsync<Gauge>(SelectAll, CancellationToken);

        // Assert
        actual.Should().BeEquivalentTo(expected with { Cache = null });
    }

    [Fact]
    public async Task Insert_ShouldRoundTripNulls_WhenNullableValuesAreNull()
    {
        // Arrange
        var expected = CreateNulls();

        // Act
        await Connection.InsertAsync(expected, CancellationToken);
        var actual = await Connection.QueryFirstOrDefaultAsync<Gauge>(SelectAll, CancellationToken);

        // Assert
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task AsString_ShouldStoreEnumName_WhenInserted()
    {
        // Arrange
        await Connection.InsertAsync(CreateMax(), CancellationToken);

        // Act
        var actual = await Connection.QueryScalarFirstOrDefaultAsync<string>("""SELECT [State] FROM [dbo].[Gauges]""", CancellationToken);

        // Assert
        actual.Should().Be("Faulted");
    }

    [Fact]
    public async Task Ignore_ShouldNotCreateColumn_WhenPropertyIsIgnored()
    {
        // Act
        var act = async () => await Connection.QueryScalarFirstOrDefaultAsync<string>("""SELECT [Cache] FROM [dbo].[Gauges]""", CancellationToken);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task InsertList_ShouldRoundTripValues_WhenUsingBinaryBulk()
    {
        // Arrange
        List<Gauge> expected = [CreateMax() with { Cache = null }, CreateNulls(), CreateNulls()];

        // Act
        await Connection.InsertAsync(expected, CancellationToken);
        var actual = await Connection.QueryAsync<Gauge>(SelectAll, CancellationToken);

        // Assert
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Bulk_ShouldUpsertAndUpdate_WhenValuesAreConverted()
    {
        // Arrange
        var existing = CreateNulls();
        await Connection.InsertAsync(existing, CancellationToken);
        var updated = existing with { Reads = uint.MaxValue, MaybeTotal = ulong.MaxValue, State = GaugeState.Running, PreviousState = GaugeState.Idle };
        var added = CreateMax() with { Cache = null };
        var bulk = new GaugeBulk();
        bulk.UpsertRange([updated, added]);
        await using var transaction = Connection.BeginTransaction();

        // Act
        await bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);
        var actual = await Connection.QueryAsync<Gauge>(SelectAll, CancellationToken);

        // Assert
        actual.Should().BeEquivalentTo([updated, added]);
    }

    [Fact]
    public async Task UpdateAndDelete_ShouldMatchRow_WhenValuesAreConverted()
    {
        // Arrange
        var existing = CreateNulls();
        await Connection.InsertAsync(existing, CancellationToken);
        var updated = existing with { Total = ulong.MaxValue, Code = 'q', State = GaugeState.Faulted };

        // Act
        var updatedRows = await Connection.UpdateAsync(updated, CancellationToken);
        var actual = await Connection.QueryFirstOrDefaultAsync<Gauge>(SelectAll, CancellationToken);
        var deletedRows = await Connection.DeleteAsync(updated, CancellationToken);

        // Assert
        updatedRows.Should().Be(1);
        actual.Should().BeEquivalentTo(updated);
        deletedRows.Should().Be(1);
    }

    [Fact]
    public async Task Query_ShouldMapStruct_WhenDtoIsRecordStruct()
    {
        // Arrange
        await Connection.InsertAsync(CreateMax(), CancellationToken);

        // Act
        var actual = await Connection.QueryAsync<GaugeTotal>("""SELECT [Reads], [Total] FROM [dbo].[Gauges]""", CancellationToken);

        // Assert
        actual.Should().Equal(new GaugeTotal(uint.MaxValue, ulong.MaxValue));
    }
}

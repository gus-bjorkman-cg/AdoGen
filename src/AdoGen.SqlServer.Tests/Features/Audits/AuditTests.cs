using AdoGen.Sample.Features.Audit;
using AwesomeAssertions.Execution;
using Bogus;

namespace AdoGen.SqlServer.Tests.Features.Audits;

public sealed class AuditTests(TestContext testContext) : TestBase(testContext)
{
    private static readonly Faker<AuditEvent> Faker = Fakers.AuditEventFaker;
    
    private readonly List<AuditEvent> _toInsert = [];
    private readonly List<AuditEvent> _toUpdate = [];
    private readonly List<AuditEvent> _toDelete = [];
    
    protected override async ValueTask InitializeAsync()
    {
        var auditEvents = Faker.Generate(40);
        _toInsert.AddRange(auditEvents.Take(10));
        
        await Connection.InsertAsync(auditEvents.Skip(10).Take(20).ToList(), CancellationToken);
        var dbAuditEvents = await Connection.QueryAsync<AuditEvent>("SELECT * FROM [log].[Audits]", CancellationToken);
        for (var i = 20; i < auditEvents.Count; i++)
        {
            var eventId = dbAuditEvents[i - 20].EventId;
            if (i < 30) _toDelete.Add(auditEvents[i] with { EventId = eventId });
            else _toUpdate.Add(auditEvents[i] with { EventId = eventId });
        }
    }

    protected override async ValueTask DisposeAsync() => await Connection.TruncateAsync<AuditEvent>(CancellationToken);

    private async ValueTask<List<AuditEvent>> GetAll() => await Connection.QueryAsync<AuditEvent>("SELECT * FROM [log].[Audits] ORDER BY EventId", CancellationToken);
    
    [Fact]
    public async Task BulkMixed_ShouldPerformAllOperations()
    {
        // Arrange
        var bulk = new AuditEventBulk(30);
        bulk.AddRange(_toInsert);
        bulk.UpdateRange(_toUpdate);
        bulk.RemoveRange(_toDelete);
        await using var transaction = (SqlTransaction)await Connection.BeginTransactionAsync(CancellationToken);
        
        // Act
        await bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);
        
        // Assert
        var actual = await GetAll();

        using var _ = new AssertionScope();
        actual.Take(10).Should().BeEquivalentTo(_toUpdate);
        actual.Skip(10).Should().BeEquivalentTo(_toInsert, e => e.Excluding(x => x.EventId));
    }
    
    [Fact]
    public async Task BulkMixed_ShouldReturnCorrectCounts()
    {
        // Arrange
        var bulk = new AuditEventBulk(30);
        bulk.AddRange(_toInsert);
        bulk.UpdateRange(_toUpdate);
        bulk.RemoveRange(_toDelete);
        await using var transaction = (SqlTransaction)await Connection.BeginTransactionAsync(CancellationToken);
        
        // Act
        var result = await bulk.SaveChangesAsync(Connection, transaction, CancellationToken);
        await transaction.CommitAsync(CancellationToken);
        
        // Assert
        result.Should().Be(new BulkApplyResult(10, 10, 10));
    }

    [Fact]
    public async Task Upsert_ShouldUpdate_WhenIdentityKeyExists()
    {
        // Arrange
        var expected = _toUpdate[0];

        // Act
        var affected = await Connection.UpsertAsync(expected, CancellationToken);

        // Assert
        affected.Should().Be(1);
        var actual = await GetAll();
        actual.Should().HaveCount(20);
        actual.Single(x => x.EventId == expected.EventId).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Upsert_ShouldInsertWithGeneratedKey_WhenIdentityKeyIsUnknown()
    {
        // Arrange
        var maxId = (await GetAll()).Max(x => x.EventId);
        var model = _toInsert[0] with { EventId = maxId + 1000 };

        // Act
        var affected = await Connection.UpsertAsync(model, CancellationToken);

        // Assert
        affected.Should().Be(1);
        var actual = await GetAll();
        actual.Should().HaveCount(21);
        actual[^1].Should().BeEquivalentTo(model, o => o.Excluding(x => x.EventId));
    }
}

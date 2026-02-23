using System.Text;
using AdoGen.Sample.Features.Audit;
using AwesomeAssertions.Execution;
using Bogus;
using Bogus.Extensions;

namespace AdoGen.Tests.Features.Audits;

public sealed class AuditTests(TestContext testContext) : TestBase(testContext)
{
    private static readonly Faker<AuditEvent> Faker = new Faker<AuditEvent>()
        .StrictMode(true)
        .RuleFor(x => x.EventId, _ => 0)
        .RuleFor(x => x.CreatedAt, f => f.Date.RecentOffset())
        .RuleFor(x => x.EventType, f => f.Lorem.Word().ClampLength(1, 50))
        .RuleFor(x => x.JsonPayload, f =>
        {
            var json = $"{{ \"data\": \"{f.Random.String2(1, 1500)}\" }}";
            return Encoding.UTF8.GetBytes(json);
        })
        .WithDefaultConstructor();
    
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
}
using System.Data;

namespace AdoGen.Sample.Features.Audit;

public sealed partial record AuditEvent(
    long EventId,
    DateTimeOffset CreatedAt,
    string EventType,
    byte[] JsonPayload) : ISqlBulkModel;

public sealed class AuditEventProfile : SqlProfile<AuditEvent>
{
    public AuditEventProfile()
    {
        Table("Audits");
        Schema("log");
        Identity(x => x.EventId);
        Key(x => x.EventId);
        RuleFor(x => x.EventType).Name("Type").NVarChar(50);
        RuleFor(x => x.JsonPayload).Type(SqlDbType.VarBinary).Size(int.MaxValue);
    }
}
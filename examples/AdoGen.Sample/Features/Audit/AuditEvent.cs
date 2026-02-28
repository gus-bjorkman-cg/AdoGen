using System.Data;

namespace AdoGen.Sample.Features.Audit;

// Created to test code with a different schema
public sealed partial record AuditEvent(
    long EventId,
    DateTimeOffset CreatedAt,
    string EventType,
    byte[] JsonPayload) : ISqlBulkModel, INpgsqlBulkModel;

public sealed class AuditEventProfile : SqlProfile<AuditEvent>
{
    public AuditEventProfile()
    {
        Table("Audits");
        Schema("log");
        Identity(x => x.EventId);
        Key(x => x.EventId);
        RuleFor(x => x.EventType).Name("Type").NVarChar(50);
        RuleFor(x => x.JsonPayload).Type(SqlDbType.VarBinary).Size(8000);
    }
}

public sealed class AuditEventNpgsqlProfile : NpgsqlProfile<AuditEvent>
{
    public AuditEventNpgsqlProfile()
    {
        Table("Audits");
        Schema("log");
        Identity(x => x.EventId);
        Key(x => x.EventId);

        RuleFor(x => x.EventType).Name("Type").Varchar(50);
        RuleFor(x => x.JsonPayload).Type(NpgsqlDbType.Bytea);
    }
}
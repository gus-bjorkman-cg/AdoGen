using AdoGen.Generator.Models;

namespace AdoGen.Generator.Emitters.PostgreSql;

internal sealed class PostgreSqlIdentifierQuoter : IIdentifierQuoter
{
    public static readonly PostgreSqlIdentifierQuoter Instance = new();
    private PostgreSqlIdentifierQuoter() { }

    public SqlProviderKind Provider => SqlProviderKind.PostgreSql;
    public string Quote(string identifier) => $"\"{identifier}\"";
    public string QuoteSchemaTable(string? schema, string table)
        => schema is { Length: > 0 } ? $"\"{schema}\".\"{table}\"" : $"\"{table}\"";
}

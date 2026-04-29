using AdoGen.Generator.Models;

namespace AdoGen.Generator.Emitters.SqlServer;

internal sealed class SqlServerIdentifierQuoter : IIdentifierQuoter
{
    public static readonly SqlServerIdentifierQuoter Instance = new();
    private SqlServerIdentifierQuoter() { }

    public SqlProviderKind Provider => SqlProviderKind.SqlServer;
    public string Quote(string identifier) => $"[{identifier}]";
    public string QuoteSchemaTable(string? schema, string table)
        => schema is { Length: > 0 } ? $"[{schema}].[{table}]" : $"[{table}]";
}


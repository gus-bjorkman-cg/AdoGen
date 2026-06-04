using AdoGen.Generator.Models;

namespace AdoGen.Generator.Emitters.PostgreSql;

internal sealed class PostgreSqlIdentifierQuoter : IIdentifierQuoter
{
    public static readonly PostgreSqlIdentifierQuoter Instance = new();
    private PostgreSqlIdentifierQuoter() { }

    bool IIdentifierQuoter.IsMatch(ValidatedDiscoveryDto discovery) =>
        discovery.Discovery.Provider == SqlProviderKind.PostgreSql;
    
    string IIdentifierQuoter.Quote(string identifier) => $"\"{identifier}\"";
    string IIdentifierQuoter.QuoteSchemaTable(string? schema, string table)
        => schema is { Length: > 0 } ? $"\"{schema}\".\"{table}\"" : $"\"{table}\"";
    
    string IIdentifierQuoter.FactoryClassName(string dtoName) => dtoName + "Npgsql";
}

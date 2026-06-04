using AdoGen.Generator.Models;

namespace AdoGen.Generator.Emitters.SqlServer;

internal sealed class SqlServerIdentifierQuoter : IIdentifierQuoter
{
    public static readonly SqlServerIdentifierQuoter Instance = new();
    private SqlServerIdentifierQuoter() { }

    bool IIdentifierQuoter.IsMatch(ValidatedDiscoveryDto discovery) =>
        discovery.Discovery.Provider == SqlProviderKind.SqlServer;
    
    string IIdentifierQuoter.Quote(string identifier) => $"[{identifier}]";
    string IIdentifierQuoter.QuoteSchemaTable(string? schema, string table)
        => schema is { Length: > 0 } ? $"[{schema}].[{table}]" : $"[{table}]";
    
    string IIdentifierQuoter.FactoryClassName(string dtoName) => dtoName + "Sql";
}


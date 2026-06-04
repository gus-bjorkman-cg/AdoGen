using AdoGen.Generator.Models;

namespace AdoGen.Generator.Emitters;

internal interface IIdentifierQuoter
{
    bool IsMatch(ValidatedDiscoveryDto discovery);
    string Quote(string identifier);
    string QuoteSchemaTable(string? schema, string table);
    string FactoryClassName(string dtoName);
}

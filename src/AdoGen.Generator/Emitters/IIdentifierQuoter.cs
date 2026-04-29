using AdoGen.Generator.Models;

namespace AdoGen.Generator.Emitters;

internal interface IIdentifierQuoter
{
    SqlProviderKind Provider { get; }
    string Quote(string identifier);
    string QuoteSchemaTable(string? schema, string table);
}

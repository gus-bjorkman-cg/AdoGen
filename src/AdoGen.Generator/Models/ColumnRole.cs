namespace AdoGen.Generator.Models;

internal enum ColumnRole
{
    Plain,
    Key,
    Identity
    // Reserved for feature: ReadOnly, ConcurrencyToken, DatabaseGenerated
}
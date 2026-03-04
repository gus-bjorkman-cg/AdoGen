namespace AdoGen.Generator.Models;

internal enum SqlModelKind : byte
{
    None = 0,
    Mapper = 1,
    Domain = 2,
    Bulk = 3
}
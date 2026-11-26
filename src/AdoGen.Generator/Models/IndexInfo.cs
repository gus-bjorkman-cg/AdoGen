using System.Collections.Immutable;

namespace AdoGen.Generator.Models;

/// <summary>
/// An index declared in a profile via <c>HasIndex(...)</c>, by CLR property name in key order.
/// </summary>
internal sealed record IndexInfo(ImmutableArray<string> Properties, bool IsUnique);

/// <summary>
/// An index resolved to quoted column names and a DDL name, ready for emission.
/// </summary>
internal sealed record IndexDefinition(string NameQuoted, ImmutableArray<string> ColumnsQuoted, bool IsUnique);

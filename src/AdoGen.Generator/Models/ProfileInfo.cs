using System.Collections.Immutable;

namespace AdoGen.Generator.Models;

internal sealed record ProfileInfo(
    string Schema,
    string Table,
    ImmutableArray<string> Keys,
    ImmutableHashSet<string> IdentityKeys,
    ImmutableDictionary<string, ParamConfig> ParamsByProperty
);

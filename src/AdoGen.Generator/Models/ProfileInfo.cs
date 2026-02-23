using System.Collections.Immutable;

namespace AdoGen.Generator.Models;

internal sealed record ProfileInfo(
    string Schema,
    string Table,
    ImmutableArray<string> Keys,
    ImmutableHashSet<string> IdentityKeys,
    ImmutableDictionary<string, ParamConfig> ParamsByProperty
)
{
    public static readonly ProfileInfo Empty = new(
        Schema: string.Empty,
        Table: string.Empty,
        Keys: ImmutableArray<string>.Empty,
        IdentityKeys: ImmutableHashSet<string>.Empty,
        ParamsByProperty: ImmutableDictionary<string, ParamConfig>.Empty
    );
}

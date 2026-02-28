using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Models;

internal sealed record ProfileInfo(
    string Schema,
    string Table,
    ImmutableArray<string> Keys,
    ImmutableHashSet<string> IdentityKeys,
    ImmutableArray<IPropertySymbol> DtoProperties,
    ImmutableDictionary<string, ParamConfig> ParamsByProperty,
    string Namespace
)
{
    public static readonly ProfileInfo Empty = new(
        Schema: string.Empty,
        Table: string.Empty,
        Keys: ImmutableArray<string>.Empty,
        IdentityKeys: ImmutableHashSet<string>.Empty,
        DtoProperties: ImmutableArray<IPropertySymbol>.Empty,
        ParamsByProperty: ImmutableDictionary<string, ParamConfig>.Empty,
        Namespace: string.Empty
    );
}

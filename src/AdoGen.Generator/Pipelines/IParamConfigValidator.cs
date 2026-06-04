using System.Collections.Immutable;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Pipelines;

internal interface IParamConfigValidator
{
    bool IsMatch(SqlProviderKind kind, PropertyTypeKind typeKind);
    void Validate(
        DiscoveryDto dto,
        IPropertySymbol property,
        ParamConfig? cfg,
        ImmutableArray<Diagnostic>.Builder diagnostics);
}
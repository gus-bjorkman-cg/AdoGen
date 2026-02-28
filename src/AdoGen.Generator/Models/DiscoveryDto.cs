using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Models;

internal readonly record struct DiscoveryDto(
    INamedTypeSymbol Dto,
    SqlModelKind Kind,
    INamedTypeSymbol? Profile,
    SemanticModel? ProfileSemanticModel,
    SqlProviderKind Provider);
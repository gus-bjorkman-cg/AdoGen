using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Models;

internal readonly record struct DiscoveryDto(
    INamedTypeSymbol Dto,
    SqlModelKind Kind,
    INamedTypeSymbol? Profile,
    SemanticModel? ProfileSemanticModel,
    SqlProviderKind Provider,
    bool ShouldGeneratePatchClass,  // true for exactly one provider per DTO — the one that owns the shared {Dto}Patch class
    int ProfileCount);              // number of profiles found for this DTO + provider; > 1 is an error

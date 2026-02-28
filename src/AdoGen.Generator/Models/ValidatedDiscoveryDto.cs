using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Models;

internal readonly record struct ValidatedDiscoveryDto(
    DiscoveryDto Discovery,
    ProfileInfo ProfileInfo,
    ImmutableArray<Diagnostic> Diagnostics);
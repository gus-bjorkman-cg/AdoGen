using System.Collections.Immutable;
using System.Threading;
using AdoGen.Generator.Models;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Parsing;

internal interface IChainMethodHandler
{
    bool IsMatch(SqlProviderKind provider, string methodName);

    void Handle(
        SemanticModel model,
        INamedTypeSymbol dtoType,
        string propertyName,
        ChainMethod chain,
        ParamConfig cfg,
        ImmutableArray<Diagnostic>.Builder diagnosticsBuilder,
        CancellationToken ct);
}

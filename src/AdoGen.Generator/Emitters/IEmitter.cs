using AdoGen.Generator.Models;
using AdoGen.Generator.Pipelines;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Emitters;

internal interface IEmitter
{
    bool IsMatch(SqlModelKind kind, SqlProviderKind provider);
    void Handle(SourceProductionContext spc, ValidatedDiscoveryDto validatedDto);
}
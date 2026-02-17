using Microsoft.CodeAnalysis;
using AdoGen.Generator.Pipelines;
using AdoGen.Generator.Emitters;

namespace AdoGen.Generator;

[Generator]
public sealed class SqlBuilderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var dtos = Discovery.DiscoverDtos(context);
        context.RegisterSourceOutput(dtos, DtoMapperEmitter.Emit);
        context.RegisterSourceOutput(dtos, SqlParameterHelpersEmitter.Emit);
        context.RegisterSourceOutput(dtos, DomainOpsEmitter.Emit);
        context.RegisterSourceOutput(dtos, BulkEmitter.Emit);
    }
}

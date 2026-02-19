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
        var validatedDtos = DiscoveryValidation.ValidateDtos(dtos);
        
        context.RegisterSourceOutput(
            validatedDtos,
            static (spc, validatedDto) =>
            {
                for (var i = 0; i < validatedDto.Diagnostics.Length; i++) spc.ReportDiagnostic(validatedDto.Diagnostics[i]);

                if (validatedDto.Discovery.Kind == SqlModelKind.None) return;
                if (validatedDto.Diagnostics.Length != 0) return;

                var dto = validatedDto.Discovery;

                DtoMapperEmitter.Emit(spc, dto);
                SqlParameterHelpersEmitter.Emit(spc, dto, validatedDto.ProfileInfo);
                DomainOpsEmitter.Emit(spc, dto, validatedDto.ProfileInfo);
                BulkEmitter.Emit(spc, dto, validatedDto.ProfileInfo);
            });
    }
}

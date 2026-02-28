using System.Collections.Generic;
using AdoGen.Generator.Emitters;
using AdoGen.Generator.Emitters.PostgreSql;
using AdoGen.Generator.Emitters.SqlServer;
using AdoGen.Generator.Pipelines;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator;

[Generator]
public sealed class SqlBuilderGenerator : IIncrementalGenerator
{
    private static readonly List<IEmitter> Emitters =
    [
        DtoMapperEmitterSqlServer.Instance,
        ParameterHelpersEmitterSqlServer.Instance,
        DomainOpsEmitterSqlServer.Instance,
        BulkEmitterSqlServer.Instance,
        
        DtoMapperEmitterNpgSql.Instance,
        ParameterHelpersEmitterNpgSql.Instance,
        DomainOpsEmitterNpgSql.Instance,
        BulkEmitterNpgSql.Instance
    ];
    
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

                foreach (var emitter in Emitters)
                {
                    if (!emitter.IsMatch(dto.Kind, dto.Provider)) continue;
                    emitter.Handle(spc, validatedDto);
                }
            });
    }
}

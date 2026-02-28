using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using AdoGen.Generator.Pipelines;
using AdoGen.Generator.Emitters;
using AdoGen.Generator.Emitters.PostgreSql;
using AdoGen.Generator.Emitters.SqlServer;
using AdoGen.Generator.Models;

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
                
                // if (dto.Provider == SqlProviderKind.SqlServer)
                // {
                //     DtoMapperEmitter.EmitSqlServer(spc, dto, validatedDto.ProfileInfo);
                //     SqlParameterHelpersEmitter.EmitSqlServer(spc, dto, validatedDto.ProfileInfo);
                //     DomainOpsEmitter.EmitSqlServer(spc, dto, validatedDto.ProfileInfo);
                //     BulkEmitter.EmitSqlServer(spc, dto, validatedDto.ProfileInfo);
                // }
                // else
                // {
                //     DtoMapperEmitter.EmitPostgreSql(spc, dto, validatedDto.ProfileInfo);
                //     SqlParameterHelpersEmitter.EmitPostgreSql(spc, dto, validatedDto.ProfileInfo);
                //     DomainOpsEmitter.EmitPostgreSql(spc, dto, validatedDto.ProfileInfo);
                //     BulkEmitter.EmitPostgreSql(spc, dto, validatedDto.ProfileInfo);
                // }
            });
    }
}

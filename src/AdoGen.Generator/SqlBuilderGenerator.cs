using Microsoft.CodeAnalysis;
using AdoGen.Generator.Pipelines;
using AdoGen.Generator.Emitters;

namespace AdoGen.Generator;

[Generator]
public sealed class SqlBuilderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Discover candidates (public/internal, non-static classes/records)
        var candidates = Discovery.CreateDtoCandidates(context);

        // --- DTO -> SqlResult mapper ---
        var sqlResultTypes = Discovery.FilterBySqlResultInterface(context, candidates);
        context.RegisterSourceOutput(sqlResultTypes, DtoMapperEmitter.Emit);

        // --- Profiles: SqlProfile<T> ---
        var profiles = Discovery.FindSqlProfiles(context);
        context.RegisterSourceOutput(profiles.Combine(context.CompilationProvider), SqlParameterHelpersEmitter.Emit);

        // Build a cross-index from DTO -> Profile for domain ops (avoid scanning trees)
        var profilesIndex = Discovery.BuildProfilesIndex(profiles); // ImmutableArray<(Dto, Profile, SemanticModel)>

        // --- Domain model ops (ISqlDomainModel<T>) ---
        var domainTypes = Discovery.FilterBySqlDomainInterface(context, candidates);
        var domainInputs = domainTypes
            .Combine(profilesIndex)
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(domainInputs, DomainOpsEmitter.Emit);
    }
}

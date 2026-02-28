using System.Collections.Immutable;
using System.Linq;
using AdoGen.Generator.Diagnostics;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Pipelines.PostgreSql;

internal sealed class BinaryValidatorNpgsql : IParamConfigValidator
{
    private BinaryValidatorNpgsql() {}
    public static BinaryValidatorNpgsql Instance { get; } = new();
    
    public bool IsMatch(SqlProviderKind kind, PropertyTypeKind typeKind) =>
        kind is SqlProviderKind.PostgreSql && typeKind is PropertyTypeKind.ByteArray;

    public void Validate(
        DiscoveryDto dto,
        IPropertySymbol property,
        ParamConfig? cfg,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        if (cfg is not null && cfg.DbType?.EnumMember == "Bytea") return;

        diagnostics.Add(Diagnostic.Create(
            cfg is null ? SqlDiagnostics.MissingRequiredParameterConfig : SqlDiagnostics.BinaryMissing,
            dto.Profile!.Locations.FirstOrDefault() ?? Location.None,
            dto.Dto.Name,
            property.Name));
    }
}
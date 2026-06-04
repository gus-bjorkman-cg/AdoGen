using System.Collections.Immutable;
using System.Linq;
using AdoGen.Generator.Diagnostics;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Pipelines.PostgreSql;

internal sealed class StringValidatorNpgsql : IParamConfigValidator
{
    private StringValidatorNpgsql() {}
    public static StringValidatorNpgsql Instance { get; } = new();
    
    public bool IsMatch(SqlProviderKind kind, PropertyTypeKind typeKind) =>
        kind is SqlProviderKind.PostgreSql && typeKind is PropertyTypeKind.String;

    public void Validate(
        DiscoveryDto dto,
        IPropertySymbol property,
        ParamConfig? cfg,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        if (cfg?.DbType != null &&
            (cfg.DbType.Value.EnumMember != "Varchar" || cfg.Size is not null))
            return;

        diagnostics.Add(Diagnostic.Create(
            cfg is null ? SqlDiagnostics.MissingRequiredParameterConfig : SqlDiagnostics.StringMissing,
            dto.Profile!.Locations.FirstOrDefault() ?? Location.None,
            dto.Dto.Name,
            property.Name));
    }
}
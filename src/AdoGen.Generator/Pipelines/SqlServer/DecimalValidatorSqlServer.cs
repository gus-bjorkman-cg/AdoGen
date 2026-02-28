using System.Collections.Immutable;
using System.Linq;
using AdoGen.Generator.Diagnostics;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Pipelines.SqlServer;

internal sealed class DecimalValidatorSqlServer : IParamConfigValidator
{
    private DecimalValidatorSqlServer() {}
    public static DecimalValidatorSqlServer Instance { get; } = new();
    
    public bool IsMatch(SqlProviderKind kind, PropertyTypeKind typeKind) =>
        kind is SqlProviderKind.SqlServer && typeKind is PropertyTypeKind.Decimal;

    public void Validate(
        DiscoveryDto dto,
        IPropertySymbol property,
        ParamConfig? cfg,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        if (cfg is not null && cfg.DbType?.EnumMember == "Decimal" && cfg.Precision is not null && cfg.Scale is not null) return;

        diagnostics.Add(Diagnostic.Create(
            cfg is null ? SqlDiagnostics.MissingRequiredParameterConfig : SqlDiagnostics.DecimalMissing,
            dto.Profile!.Locations.FirstOrDefault() ?? Location.None,
            dto.Dto.Name,
            property.Name));
    }
}
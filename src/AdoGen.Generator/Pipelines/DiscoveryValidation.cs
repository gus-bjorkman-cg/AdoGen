using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AdoGen.Generator.Diagnostics;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;
using AdoGen.Generator.Parsing;
using AdoGen.Generator.Pipelines.PostgreSql;
using AdoGen.Generator.Pipelines.SqlServer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AdoGen.Generator.Pipelines;

internal static class DiscoveryValidation
{
    private static readonly List<IParamConfigValidator> ParamConfigValidators =
    [
        StringValidatorSqlServer.Instance,
        DecimalValidatorSqlServer.Instance,
        BinaryValidatorSqlServer.Instance,
        StringValidatorNpgsql.Instance,
        DecimalValidatorNpgsql.Instance,
        BinaryValidatorNpgsql.Instance
    ];
    
    internal static IncrementalValuesProvider<ValidatedDiscoveryDto> ValidateDtos(IncrementalValuesProvider<DiscoveryDto> dtos)
    {
        var initial = dtos.Select(static (dto, ct) =>
        {
            ct.ThrowIfCancellationRequested();

            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

            var isPartial = dto.Dto.DeclaringSyntaxReferences
                .Select(r => r.GetSyntax(ct))
                .OfType<TypeDeclarationSyntax>()
                .Any(t => t.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));

            if (!isPartial)
            {
                diagnostics.Add(Diagnostic.Create(
                    SqlDiagnostics.NotPartial,
                    dto.Dto.Locations.FirstOrDefault() ?? Location.None,
                    dto.Dto.Name));
            }

            if (dto.Profile is null || dto.ProfileSemanticModel is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    SqlDiagnostics.MissingProfile,
                    dto.Dto.Locations.FirstOrDefault() ?? Location.None,
                    dto.Dto.Name));
            }

            return new ValidatedDiscoveryDto(dto, ProfileInfo.Empty, diagnostics.ToImmutable());
        });
        
        return initial.Select(static (vdto, ct) =>
        {
            ct.ThrowIfCancellationRequested();

            if (vdto.Diagnostics.Length > 0) return vdto;

            var dto = vdto.Discovery;

            var props = dto.Dto.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
                .OrderBy(x =>
                {
                    var loc = x.Locations.FirstOrDefault(l => l.IsInSource);
                    return loc is null ? int.MaxValue : loc.SourceSpan.Start;
                })
                .ThenBy(x => x.Name, StringComparer.Ordinal)
                .ToImmutableArray();
            
            var propsNeedingConfig = new Dictionary<IPropertySymbol, PropertyTypeKind>(props.Length, SymbolEqualityComparer.Default);

            for (var i = 0; i < props.Length; i++)
            {
                var p = props[i];
                if (p.Type.IsString) propsNeedingConfig.Add(p, PropertyTypeKind.String);
                else if (p.Type.IsDecimal) propsNeedingConfig.Add(p, PropertyTypeKind.Decimal);
                else if (p.Type.IsByteArray) propsNeedingConfig.Add(p, PropertyTypeKind.ByteArray);
            }
            
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            
            if (dto.Profile is null || dto.ProfileSemanticModel is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    SqlDiagnostics.MissingProfile,
                    dto.Dto.Locations.FirstOrDefault() ?? Location.None,
                    dto.Dto.Name));

                return vdto with { Diagnostics = diagnostics.ToImmutable() };
            }

            var profile = ProfileInfoCollector.Resolve(dto, diagnostics, props, ct);
            
            if (propsNeedingConfig.Count == 0) return vdto with { ProfileInfo = profile };
            
            foreach (var kvp in propsNeedingConfig)
            {
                var p = kvp.Key;
                var typeKind = kvp.Value;
                profile.ParamsByProperty.TryGetValue(p.Name, out var cfg);

                ParamConfigValidators
                    .FirstOrDefault(x => x.IsMatch(dto.Provider, typeKind))
                    ?.Validate(dto, p, cfg, diagnostics);
            }

            return vdto with { ProfileInfo = profile, Diagnostics = diagnostics.ToImmutable() };
        });
    }
}
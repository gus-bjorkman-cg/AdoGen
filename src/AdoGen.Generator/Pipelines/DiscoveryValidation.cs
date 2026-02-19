using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using AdoGen.Generator.Diagnostics;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;
using AdoGen.Generator.Parsing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AdoGen.Generator.Pipelines;

internal static class DiscoveryValidation
{
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
                .ToArray();

            var propsNeedingConfig = new List<IPropertySymbol>(props.Length);
            for (var i = 0; i < props.Length; i++)
            {
                var p = props[i];
                var t = p.Type;

                if (t.IsString() || t.IsDecimal() || t.IsByteArray()) propsNeedingConfig.Add(p);
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

            var profile = ProfileInfoCollector.Resolve(dto, diagnostics);
            
            if (propsNeedingConfig.Count == 0) return vdto with { ProfileInfo = profile };
            
            foreach (var p in propsNeedingConfig)
            {
                if (!profile.ParamsByProperty.TryGetValue(p.Name, out var cfg))
                {
                    diagnostics.Add(Diagnostic.Create(
                        SqlDiagnostics.MissingRequiredParameterConfig,
                        dto.Profile.Locations.FirstOrDefault()
                            ?? dto.Dto.Locations.FirstOrDefault()
                            ?? Location.None,
                        dto.Dto.Name,
                        p.Name));
                    continue;
                }

                if (p.Type.IsString() && (cfg.DbType is null || cfg.Size is null))
                {
                    diagnostics.Add(Diagnostic.Create(
                        SqlDiagnostics.StringMissing,
                        dto.Profile.Locations.FirstOrDefault() ?? Location.None,
                        dto.Dto.Name,
                        p.Name));
                }

                if (p.Type.IsDecimal() && (cfg.DbType != SqlDbType.Decimal || cfg.Precision is null || cfg.Scale is null))
                {
                    diagnostics.Add(Diagnostic.Create(
                        SqlDiagnostics.DecimalMissing,
                        dto.Profile.Locations.FirstOrDefault() ?? Location.None,
                        dto.Dto.Name,
                        p.Name));
                }

                if (p.Type.IsByteArray() && (cfg.DbType is null || cfg.Size is null))
                {
                    diagnostics.Add(Diagnostic.Create(
                        SqlDiagnostics.BinaryMissing,
                        dto.Profile.Locations.FirstOrDefault() ?? Location.None,
                        dto.Dto.Name,
                        p.Name));
                }
            }

            return vdto with { ProfileInfo = profile, Diagnostics = diagnostics.ToImmutable() };
        });
    }
}
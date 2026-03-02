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

            if (!dto.Dto.IsPartial)
            {
                diagnostics.Add(Diagnostic.Create(
                    SqlDiagnostics.NotPartial,
                    dto.Dto.Locations.FirstOrDefault() ?? Location.None,
                    dto.Dto.Name));
            }

            if (dto.Dto.IsStatic)
            {
                diagnostics.Add(Diagnostic.Create(
                    SqlDiagnostics.StaticNotAllowed,
                    dto.Dto.Locations.FirstOrDefault() ?? Location.None,
                    dto.Dto.Name));
            }

            if (dto.Dto.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            {
                diagnostics.Add(Diagnostic.Create(
                    SqlDiagnostics.InvalidAccessibility,
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
            var dtoProperties = dto.Dto.GetOrderedProperties();
            
            var propsNeedingConfig = new Dictionary<IPropertySymbol, PropertyTypeKind>(dtoProperties.Length, SymbolEqualityComparer.Default);

            for (var i = 0; i < dtoProperties.Length; i++)
            {
                var property = dtoProperties[i];
                
                if (property.Type.IsString) propsNeedingConfig.Add(property, PropertyTypeKind.String);
                else if (property.Type.IsDecimal) propsNeedingConfig.Add(property, PropertyTypeKind.Decimal);
                else if (property.Type.IsByteArray) propsNeedingConfig.Add(property, PropertyTypeKind.ByteArray);
            }
            
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            var profile = ProfileInfoCollector.Resolve(dto, diagnostics, dtoProperties, ct);
            
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
    
    extension(INamedTypeSymbol type)
    {
        private bool IsPartial =>
            type.DeclaringSyntaxReferences
                .Select(static x => x.GetSyntax())
                .OfType<TypeDeclarationSyntax>()
                .Any(static x => x.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword)));

        private ImmutableArray<IPropertySymbol> GetOrderedProperties() =>
            type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(static x => x.DeclaredAccessibility == Accessibility.Public)
                .Where(static x => !x.IsStatic)
                .OrderBy(static x =>
                {
                    var loc = x.Locations.FirstOrDefault(static l => l.IsInSource);
                    return loc is null ? int.MaxValue : loc.SourceSpan.Start;
                })
                .ThenBy(static x => x.Name, StringComparer.Ordinal)
                .ToImmutableArray();
    }
}
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AdoGen.Generator.Pipelines;

internal static class Discovery
{
    private const string AbstractionsLib = "AdoGen.Abstractions";
    private const string SqlResultInterface = $"{AbstractionsLib}.ISqlResult";
    private const string SqlDomainInterface = $"{AbstractionsLib}.ISqlDomainModel";
    private const string SqlProfile = nameof(SqlProfile);

    public static IncrementalValuesProvider<INamedTypeSymbol> CreateDtoCandidates(IncrementalGeneratorInitializationContext context)
        => context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                static (ctx, ct) => ctx.SemanticModel.GetDeclaredSymbol((TypeDeclarationSyntax)ctx.Node, ct) as INamedTypeSymbol)
            .Where(static x => x is not null)
            .Where(static x => x!.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
            .Where(static x => !x!.IsStatic)
            .Select(static (x, _) => x!)
            .WithComparer(SymbolEqualityComparer.Default);

    public static IncrementalValuesProvider<(INamedTypeSymbol dto, bool implements, bool missingInterface)>
        FilterBySqlResultInterface(IncrementalGeneratorInitializationContext context,
                          IncrementalValuesProvider<INamedTypeSymbol> candidates)
        => candidates
            .Combine(context.CompilationProvider)
            .Select(static (pair, _) =>
            {
                var (typeSymbol, compilation) = pair;
                var sqlResultInterface = compilation.GetTypeByMetadataName(SqlResultInterface);
                
                if (sqlResultInterface is null) return (typeSymbol, implements: false, missingInterface: true);

                var implements = typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, sqlResultInterface));
                
                return (typeSymbol, implements, missingInterface: false);
            })
            .Where(static p => p.implements);

    public static IncrementalValuesProvider<(INamedTypeSymbol dto, bool implements, bool missingInterface)>
        FilterBySqlDomainInterface(IncrementalGeneratorInitializationContext context,
            IncrementalValuesProvider<INamedTypeSymbol> candidates)
        => candidates
            .Combine(context.CompilationProvider)
            .Select(static (pair, _) =>
            {
                var (typeSymbol, compilation) = pair;
                var sqlDomainInterface = compilation.GetTypeByMetadataName(SqlDomainInterface);
                
                if (sqlDomainInterface is null) return (typeSymbol, implements: false, missingInterface: true);

                var implements = typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, sqlDomainInterface));
                
                return (typeSymbol, implements, missingInterface: false);
            })
            .Where(static p => p.implements);
    
    public static IncrementalValuesProvider<(INamedTypeSymbol Profile, SemanticModel Model)>
        FindSqlProfiles(IncrementalGeneratorInitializationContext context)
        => context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, ct) =>
                {
                    if (ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node, ct) is not INamedTypeSymbol symbol) 
                        return (null!, ctx.SemanticModel);

                    var baseType = symbol.BaseType;
                    
                    if (baseType is null || 
                        baseType.Name != SqlProfile || 
                        baseType.TypeArguments.Length != 1 || 
                        baseType.ContainingNamespace?.ToDisplayString() != AbstractionsLib) 
                        return (null!, ctx.SemanticModel);

                    return (symbol, ctx.SemanticModel);
                })
            .Where(static x => x.symbol is not null)
            .Select(static (x, _) => (x.symbol!, x.SemanticModel));

    public static IncrementalValueProvider<ImmutableArray<(INamedTypeSymbol Dto, INamedTypeSymbol Profile, SemanticModel Model)>>
        BuildProfilesIndex(IncrementalValuesProvider<(INamedTypeSymbol Profile, SemanticModel Model)> profiles)
        => profiles
            .Select(static (p, _) => (Dto: (INamedTypeSymbol)p.Profile.BaseType!.TypeArguments[0], p.Profile, p.Model))
            .Collect();
}

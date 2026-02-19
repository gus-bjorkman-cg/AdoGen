using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Data.SqlClient;

namespace AdoGen.Generator.Tests;

public class GenerationTests
{
    private static PortableExecutableReference FromAssembly(Assembly assembly) =>
        MetadataReference.CreateFromFile(assembly.Location);
    
    [Fact]
    public Task SqlResult_ShouldRenderCorrectly()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(Source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: [syntaxTree],
            references: GetReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        
        var generator = new SqlBuilderGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var driverDiagnostics);
        var runResult = driver.GetRunResult();
        var allDiagnostics = driverDiagnostics.Concat(runResult.Diagnostics).ToArray();
        
        if (allDiagnostics.Length > 0)
        {
            var formatted = string.Join(Environment.NewLine, allDiagnostics.Select(d => d.ToString()));
            Assert.Fail("No sources were generated.\nDiagnostics:\n" + formatted);
        }
        
        var generated = runResult.Results
            .SelectMany(r => r.GeneratedSources)
            .Select(s => new
            {
                s.HintName,
                Source = s.SourceText.ToString()
            })
            .OrderBy(x => x.HintName, StringComparer.Ordinal)
            .ToArray();

        
        return Verify(generated);
    }
    
    private static ImmutableArray<MetadataReference> GetReferences()
    {
        var refs = new List<MetadataReference>(capacity: 128);

        foreach (var r in GetTrustedPlatformAssemblyReferences()) refs.Add(r);

        refs.Add(FromAssembly(typeof(Abstractions.ISqlBulkModel).Assembly));
        refs.Add(FromAssembly(typeof(SqlBulkCopy).Assembly));

        return [..refs];
    }
    
    private static IEnumerable<MetadataReference> GetTrustedPlatformAssemblyReferences()
    {
        var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        
        if (string.IsNullOrWhiteSpace(tpa)) yield break;

        foreach (var path in tpa.Split(Path.PathSeparator))
            if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) 
                yield return MetadataReference.CreateFromFile(path);
    }

    private const string Source =
        """
        using System;
        using AdoGen.Abstractions;
        
        namespace AdoGen.Generator.Tests;
        
        public sealed partial record User(Guid Id, string Name, string Email) : ISqlBulkModel;
        
        public sealed class UserProfile : SqlProfile<User>
        {
            public UserProfile()
            {
                RuleFor(x => x.Name).VarChar(20);
                RuleFor(x => x.Email).VarChar(50);
            }
        }
        """;
}
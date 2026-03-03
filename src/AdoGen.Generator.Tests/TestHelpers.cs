using System.Collections.Immutable;
using System.Reflection;
using AdoGen.PostgreSql;
using AdoGen.SqlServer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace AdoGen.Generator.Tests;

internal static class TestHelpers
{
    private static readonly GeneratorDriver Driver = CSharpGeneratorDriver.Create(new SqlBuilderGenerator());
    private static PortableExecutableReference GetReference(this Assembly assembly) =>
        MetadataReference.CreateFromFile(assembly.Location);
    
    public static RunResult RunGenerator(this string source, ProviderKind provider)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            assemblyName: $"Tests{provider.ToString()}",
            syntaxTrees: [syntaxTree],
            references: GetReferences(provider),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        
        var driver = Driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var driverDiagnostics);
        var runResult = driver.GetRunResult();
        var diagnostics = driverDiagnostics.Concat(runResult.Diagnostics).ToImmutableArray();
        
        return new RunResult(runResult, diagnostics);
    }

    public static string GetGeneratedText(this GeneratorDriverRunResult result, string fileName) =>
        result.Results
            .SelectMany(x => x.GeneratedSources)
            .Where(x => x.HintName == fileName)
            .Select(x => x.SourceText.ToString())
            .FirstOrDefault() ?? "";

    public static string GetFileName(this string dtoName, FileKind fileKind, ProviderKind provider)
    {
        var suffix = fileKind switch
        {
            FileKind.Parameters => "",
            FileKind.Mapper => "Mapper",
            FileKind.Domain => "DomainOps",
            FileKind.Bulk => "Bulk",
            _ => throw new ArgumentOutOfRangeException(nameof(fileKind), fileKind, null)
        };
        
        var providerSuffix = provider switch
        {
            ProviderKind.SqlServer => "Sql",
            ProviderKind.PostgreSql => "Npgsql",
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };
        
        return $"{dtoName}{suffix}.{providerSuffix}.g.cs";
    }
    
    private static ImmutableArray<MetadataReference> GetReferences(ProviderKind provider) => provider switch
    {
        ProviderKind.SqlServer => SqlServerReferences!.Value,
        ProviderKind.PostgreSql => NpgsqlReferences!.Value,
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
    };

    private static ImmutableArray<MetadataReference>? SqlServerReferences
    {
        get
        {
            if (field is not null) return field.Value;
            
            var result = ImmutableArray.CreateBuilder<MetadataReference>();
            result.AddRange(TrustedPlatformAssemblyReferences!.Value);
            result.Add(typeof(ISqlBulkModel).Assembly.GetReference());
            result.Add(typeof(SqlBulkCopy).Assembly.GetReference());
            field = result.ToImmutable();
            
            return field.Value;
        }
    }

    private static ImmutableArray<MetadataReference>? NpgsqlReferences
    {
        get
        {
            if (field is not null) return field.Value;
            
            var result = ImmutableArray.CreateBuilder<MetadataReference>();
            result.AddRange(TrustedPlatformAssemblyReferences!.Value);
            result.Add(typeof(INpgsqlBulkModel).Assembly.GetReference());
            result.Add(typeof(NpgsqlConnection).Assembly.GetReference());
            field = result.ToImmutable();
            
            return field.Value;
        }
    }

    private static ImmutableArray<MetadataReference>? TrustedPlatformAssemblyReferences
    {
        get
        {
            if (field is not null) return field.Value;
            
            var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        
            if (string.IsNullOrWhiteSpace(trustedPlatformAssemblies)) return [];
            
            field = trustedPlatformAssemblies
                .Split(Path.PathSeparator)
                .Where(static x => x.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .Select(static MetadataReference (x) => MetadataReference.CreateFromFile(x))
                .ToImmutableArray();
            
            return field.Value;
        }
    }
}

public sealed record RunResult(GeneratorDriverRunResult Result, ImmutableArray<Diagnostic> Diagnostics);

internal enum ProviderKind : byte
{
    SqlServer = 1,
    PostgreSql = 2
}

internal enum FileKind : byte
{
    Parameters = 1,
    Mapper = 2,
    Domain = 3,
    Bulk = 4
}

internal readonly record struct AdoGenInterface
{
    private const string ProviderSqlServer = "SqlServer";
    private const string ProviderNpgsql = "PostgreSql";
    
    private const string NamespaceSqlServer = "AdoGen.SqlServer";
    private const string NamespaceNpgsql = "AdoGen.PostgreSql";
    
    private const string ProfileNameSqlServer = "SqlProfile";
    private const string ProfileNameNpgsql = "NpgsqlProfile";
    
    public string Provider { get; }
    public string Namespace { get; }
    public string Interface { get; }
    public string ProfileName { get; }

    private AdoGenInterface(string provider, string @namespace, string @interface, string profileName)
    {
        Provider = provider;
        Namespace = @namespace;
        Interface = @interface;
        ProfileName = profileName;
    }
    
    public static readonly AdoGenInterface SqlResult = new(ProviderSqlServer, NamespaceSqlServer, "ISqlResult", ProfileNameSqlServer);
    public static readonly AdoGenInterface SqlDomainModel = new(ProviderSqlServer, NamespaceSqlServer, "ISqlDomainModel", ProfileNameSqlServer);
    public static readonly AdoGenInterface SqlBulkModel = new(ProviderSqlServer, NamespaceSqlServer, "ISqlBulkModel", ProfileNameSqlServer);
    
    public static readonly AdoGenInterface NpgsqlResult = new(ProviderNpgsql, NamespaceNpgsql, "INpgsqlResult", ProfileNameNpgsql);
    public static readonly AdoGenInterface NpgsqlDomainModel = new(ProviderNpgsql, NamespaceNpgsql, "INpgsqlDomainModel", ProfileNameNpgsql);
    public static readonly AdoGenInterface NpgsqlBulkModel = new(ProviderNpgsql, NamespaceNpgsql, "INpgsqlBulkModel", ProfileNameNpgsql);
}
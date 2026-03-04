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
    
    public static RunResult RunGenerator(this string source, AdoGenType genType)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            assemblyName: $"Tests{genType.ToString()}",
            syntaxTrees: [syntaxTree],
            references: GetReferences(genType),
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

    private static ImmutableArray<MetadataReference> GetReferences(AdoGenType genType)
    {
        if (genType.Provider.Name == DbProvider.SqlServer) return SqlServerReferences!.Value;
        if (genType.Provider.Name == DbProvider.PostgreSql) return NpgsqlReferences!.Value;
        
        throw new InvalidOperationException($"Unknown provider {genType.Provider.Name}");
    }

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

internal readonly record struct DbProvider
{
    public string Name { get; }
    public string ExtensionName { get; }
    
    private DbProvider(string name, string extensionName)
    {
        Name = name;
        ExtensionName = extensionName;
    }
    
    public static readonly DbProvider SqlServer = new("SqlServer", "Sql");
    public static readonly DbProvider PostgreSql = new("PostgreSql", "Npgsql");
    
    public static implicit operator string(DbProvider provider) => provider.Name;
}

internal readonly record struct AdoGenType
{
    private const string NamespaceSqlServer = "AdoGen.SqlServer";
    private const string NamespaceNpgsql = "AdoGen.PostgreSql";
    
    private const string ProfileNameSqlServer = "SqlProfile";
    private const string ProfileNameNpgsql = "NpgsqlProfile";
    
    private const string FileNameMapper = "Mapper";
    private const string FileNameDomain = "Domain";
    private const string FileNameBulk = "Bulk";
    
    public string FileName { get; }
    public DbProvider Provider { get; }
    public string Namespace { get; }
    public string Interface { get; }
    public string ProfileName { get; }

    private AdoGenType(string fileName, DbProvider provider, string @namespace, string @interface, string profileName)
    {
        FileName = fileName;
        Provider = provider;
        Namespace = @namespace;
        Interface = @interface;
        ProfileName = profileName;
    }
    
    public static readonly AdoGenType SqlMapper = new(FileNameMapper, DbProvider.SqlServer, NamespaceSqlServer, "ISqlMapper", ProfileNameSqlServer);
    public static readonly AdoGenType SqlDomainModel = new(FileNameDomain, DbProvider.SqlServer, NamespaceSqlServer, "ISqlDomainModel", ProfileNameSqlServer);
    public static readonly AdoGenType SqlBulkModel = new(FileNameBulk, DbProvider.SqlServer, NamespaceSqlServer, "ISqlBulkModel", ProfileNameSqlServer);
    
    public static readonly AdoGenType NpgsqlMapper = new(FileNameMapper, DbProvider.PostgreSql, NamespaceNpgsql, "INpgsqlMapper", ProfileNameNpgsql);
    public static readonly AdoGenType NpgsqlDomainModel = new(FileNameDomain, DbProvider.PostgreSql, NamespaceNpgsql, "INpgsqlDomainModel", ProfileNameNpgsql);
    public static readonly AdoGenType NpgsqlBulkModel = new(FileNameBulk, DbProvider.PostgreSql, NamespaceNpgsql, "INpgsqlBulkModel", ProfileNameNpgsql);
}
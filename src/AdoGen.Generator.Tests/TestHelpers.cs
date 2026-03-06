using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using AdoGen.PostgreSql;
using AdoGen.SqlServer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Data.SqlClient;
using Npgsql;
using Xunit.Abstractions;

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
            assemblyName: $"{genType.FileName}.{genType.Provider.ExtensionName}.Tests",
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
    
    extension(AdoGenType genType)
    {
        public AdoGenType GetMapperType() =>
            AdoGenType
                .GetAll()
                .Where(x => x.Provider == genType.Provider)
                .First(x => x.FileName == AdoGenType.FileNameMapper);

        public AdoGenType GetDomainType() =>
            AdoGenType
                .GetAll()
                .Where(x => x.Provider == genType.Provider)
                .First(x => x.FileName == AdoGenType.FileNameDomain);

        public AdoGenType GetBulkType() =>
            AdoGenType
                .GetAll()
                .Where(x => x.Provider == genType.Provider)
                .First(x => x.FileName == AdoGenType.FileNameBulk);
    }
}

public sealed record RunResult(GeneratorDriverRunResult Result, ImmutableArray<Diagnostic> Diagnostics);

public readonly record struct DbProvider
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

public readonly record struct AdoGenType : IXunitSerializable
{
    private static readonly List<AdoGenType> Items = [];
    public static IReadOnlyList<AdoGenType> GetAll() => Items;
    
    private const string NamespaceSqlServer = "AdoGen.SqlServer";
    private const string NamespaceNpgsql = "AdoGen.PostgreSql";
    
    private const string ProfileNameSqlServer = "SqlProfile";
    private const string ProfileNameNpgsql = "NpgsqlProfile";
    
    public const string FileNameMapper = "Mapper";
    public const string FileNameDomain = "Domain";
    public const string FileNameBulk = "Bulk";
    
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
        
        Items.Add(this);
    }

    public static readonly AdoGenType SqlMapper = new(FileNameMapper, DbProvider.SqlServer, NamespaceSqlServer, "ISqlMapper", ProfileNameSqlServer);
    public static readonly AdoGenType SqlDomainModel = new(FileNameDomain, DbProvider.SqlServer, NamespaceSqlServer, "ISqlDomainModel", ProfileNameSqlServer);
    public static readonly AdoGenType SqlBulkModel = new(FileNameBulk, DbProvider.SqlServer, NamespaceSqlServer, "ISqlBulkModel", ProfileNameSqlServer);
    
    public static readonly AdoGenType NpgsqlMapper = new(FileNameMapper, DbProvider.PostgreSql, NamespaceNpgsql, "INpgsqlMapper", ProfileNameNpgsql);
    public static readonly AdoGenType NpgsqlDomainModel = new(FileNameDomain, DbProvider.PostgreSql, NamespaceNpgsql, "INpgsqlDomainModel", ProfileNameNpgsql);
    public static readonly AdoGenType NpgsqlBulkModel = new(FileNameBulk, DbProvider.PostgreSql, NamespaceNpgsql, "INpgsqlBulkModel", ProfileNameNpgsql);
    
    public void Deserialize(IXunitSerializationInfo info) => 
        Unsafe.AsRef(in this) = Items.First(x => x.Interface == info.GetValue<string>(nameof(Interface)));

    public void Serialize(IXunitSerializationInfo info) => info.AddValue(nameof(Interface), Interface);
    public override string ToString() => Interface;
}

public readonly record struct TestTypes : IXunitSerializable
{
    private readonly List<TestTypes> _items = [];
    public string Name { get; }
    
    private TestTypes(string name)
    {
        Name = name;
        _items.Add(this);
    }

    public static readonly TestTypes User = new(nameof(User));
    public static readonly TestTypes AuditEvent = new(nameof(AuditEvent));
    public static readonly TestTypes TestType = new(nameof(TestType));

    public void Deserialize(IXunitSerializationInfo info) => 
        Unsafe.AsRef(in this) = _items.First(x => x.Name == info.GetValue<string>(nameof(Name)));

    public void Serialize(IXunitSerializationInfo info) => info.AddValue(nameof(Name), Name);
    
    public override string ToString() => Name;
}

public interface ITestTypeSource
{
    bool IsMatch(TestTypes type);
    string Handle(AdoGenType genType);
}

internal sealed class UserSourceHandler : ITestTypeSource
{
    private UserSourceHandler() {}
    public static UserSourceHandler Instance { get; } = new();
    
    public bool IsMatch(TestTypes type) => type == TestTypes.User;

    public string Handle(AdoGenType genType) =>
        $$"""
          using {{genType.Namespace}};

          namespace AdoGen.Generator.Tests;

          public sealed partial record User(Guid Id, string Name, string Email) : {{genType.Interface}};

          public sealed class UserProfile : {{genType.ProfileName}}<User>
          {
              public UserProfile()
              {
                  RuleFor(x => x.Name).VarChar(20);
                  RuleFor(x => x.Email).VarChar(50);
              }
          }
          """;
}

internal sealed class TestTypeSourceHandler : ITestTypeSource
{
    private TestTypeSourceHandler() {}
    public static TestTypeSourceHandler Instance { get; } = new();
    
    public bool IsMatch(TestTypes type) => type == TestTypes.TestType;

    public string Handle(AdoGenType genType) =>
        $$"""
          using {{genType.Namespace}};
          using System.Data;

          namespace AdoGen.Generator.Tests;

          public enum Fruits
          {
              Apple,
              Banana,
              Orange
          }
          
          [Flags]
          public enum Flags
          {
              None = 0,
              Flag1 = 1,
              Flag2 = 2,
              Flag3 = 4
          }
          
          public enum ByteEnum : byte
          {
              Option1 = 1,
              Option2 = 2,
              Option3 = 3
          }
          
          public enum ShortEnum : short
          {
              Value1 = 1,
              Value2 = 2,
              Value3 = 3
          }
          
          public enum IntEnum : int
          {
              Item1 = 1,
              Item2 = 2,
              Item3 = 3
          }
          
          public enum LongEnum : long
          {
              ValueA = 1,
              ValueB = 2,
              ValueC = 3
          }
          
          public sealed partial record TestType(
              int Int,
              int? NullableInt,
              decimal Decimal,
              decimal? NullableDecimal,
              Guid? NullableGuid,
              string? NullableStringVarchar,
              string? NullableStringNVarchar,
              string StringVarcharRuledNull,
              string CharString,
              string NCharString,
              float Float,
              float? NullableFloat,
              DateTime DateTime,
              DateTime NullableDateTime,
              double Double,
              double? NullableDouble,
              char Char,
              char NChar,
              char? NullableChar,
              byte[]? NullableBytes,
              byte[] Bytes,
              Fruits Fruits,
              Flags Flags,
              ByteEnum ByteEnum,
              ShortEnum ShortEnum,
              IntEnum IntEnum,
              LongEnum LongEnum
              ) : {{genType.Interface}};
          
          {{Profile(genType)}}
          """;

    private static string Profile(AdoGenType genType) =>
        genType.Provider == DbProvider.SqlServer
            ? """
              public sealed class TestTypeProfile : SqlProfile<TestType>
              {
                  public TestTypeProfile()
                  {
                      Key(x => x.Int);
                      Key(x => x.Decimal);
                      
                      RuleFor(x => x.NullableStringVarchar).VarChar(100);
                      RuleFor(x => x.NullableStringNVarchar).NVarChar(100);
                      RuleFor(x => x.StringVarcharRuledNull).VarChar(100).Nullable();
                      RuleFor(x => x.Char).Char(1);
                      RuleFor(x => x.NChar).NChar(1);
                      RuleFor(x => x.NullableChar).NChar(1).Nullable();
                      RuleFor(x => x.NullableBytes).VarBinary(200).Nullable();
                      RuleFor(x => x.Bytes).VarBinary(200);
                      RuleFor(x => x.Decimal).Decimal(4, 2);
                      RuleFor(x => x.NullableDecimal).Decimal(6, 3).Nullable();
                      RuleFor(x => x.CharString).Char(10);
                      RuleFor(x => x.NCharString).NChar(15);
                      RuleFor(x => x.NullableDateTime).Type(SqlDbType.DateTime).Nullable();
                  }
              }
              """
            : """
              public sealed class TestTypeNpgsqlProfile : NpgsqlProfile<TestType>
              {
                  public TestTypeNpgsqlProfile()
                  {
                      Key(x => x.Int);
                      Key(x => x.Decimal);
              
                      RuleFor(x => x.NullableStringVarchar).VarChar(100);
                      RuleFor(x => x.NullableStringNVarchar).VarChar(100);
                      RuleFor(x => x.StringVarcharRuledNull).VarChar(100).Nullable();
                      RuleFor(x => x.Char).Char(1);
                      RuleFor(x => x.NChar).Char(1);
                      RuleFor(x => x.NullableChar).Char(1).Nullable();
                      RuleFor(x => x.NullableBytes).Bytea().Nullable();
                      RuleFor(x => x.Bytes).Bytea();
                      RuleFor(x => x.Decimal).Decimal(4, 2);
                      RuleFor(x => x.NullableDecimal).Decimal(6, 3).Nullable();
                      RuleFor(x => x.CharString).Char(10);
                      RuleFor(x => x.NCharString).Char(15);
                      RuleFor(x => x.NullableDateTime).Type(NpgsqlDbType.Timestamp).Nullable();
                  }
              }
              """;
}

internal sealed class AuditEventSourceHandler : ITestTypeSource
{
    private AuditEventSourceHandler() {}
    public static AuditEventSourceHandler Instance { get; } = new();
    
    public bool IsMatch(TestTypes type) => type == TestTypes.AuditEvent;

    public string Handle(AdoGenType genType) =>
        $"""
         using {genType.Namespace};
         using System;
         using System.Data;

         namespace AdoGen.Generator.Tests;

         public sealed partial record AuditEvent(
             long EventId,
             DateTimeOffset CreatedAt,
             string EventType,
             byte[] JsonPayload) : {genType.Interface};
             
         {Profile(genType)}
         """;
    
    private static string Profile(AdoGenType genType) =>
        genType.Provider == DbProvider.SqlServer
            ? """
              public sealed class AuditEventProfile : SqlProfile<AuditEvent>
              {
                  public AuditEventProfile()
                  {
                      Table("Audits");
                      Schema("log");
                      Identity(x => x.EventId);
                      Key(x => x.EventId);
                      RuleFor(x => x.EventType).Name("Type").NVarChar(50);
                      RuleFor(x => x.JsonPayload).Type(SqlDbType.VarBinary).Size(8000);
                  }
              }
              """
            : """
              public sealed class AuditEventNpgsqlProfile : NpgsqlProfile<AuditEvent>
              {
                  public AuditEventNpgsqlProfile()
                  {
                      Table("Audits");
                      Schema("log");
                      Identity(x => x.EventId);
                      Key(x => x.EventId);
                      RuleFor(x => x.EventType).Name("Type").VarChar(50);
                      RuleFor(x => x.JsonPayload).Type(NpgsqlDbType.Bytea);
                  }
              }
              """;
}
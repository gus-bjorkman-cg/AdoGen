using System.Collections.Immutable;
using System.Reflection;
using AdoGen.PostgreSql;
using AdoGen.SqlServer;
using Microsoft.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace AdoGen.Generator.Tests;

internal static class ModuleInitializer
{
    private static PortableExecutableReference GetReference(this Assembly assembly) =>
        MetadataReference.CreateFromFile(assembly.Location);

    public static ImmutableArray<MetadataReference> SqlServerReferences { get; }
    public static ImmutableArray<MetadataReference> NpgsqlReferences { get; }

    static ModuleInitializer()
    {
        DerivePathInfo(
            (sourceFile, _, type, method) =>
                new PathInfo(
                    directory: Path.Combine(Path.GetDirectoryName(sourceFile)!, "Snapshots"),
                    typeName: type.Name,
                    methodName: method.Name));

        var trustedPlatformAssembyAsString = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "";

        var trustedPlatformAssemblies = trustedPlatformAssembyAsString
            .Split(Path.PathSeparator)
            .Where(static x => x.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(static MetadataReference (x) => MetadataReference.CreateFromFile(x))
            .ToImmutableArray();

        var sqlServerReferences = ImmutableArray.CreateBuilder<MetadataReference>();
        sqlServerReferences.AddRange(trustedPlatformAssemblies);
        sqlServerReferences.Add(typeof(ISqlBulkModel).Assembly.GetReference());
        sqlServerReferences.Add(typeof(SqlBulkCopy).Assembly.GetReference());
        SqlServerReferences = sqlServerReferences.ToImmutable();

        var npgsqlReferences = ImmutableArray.CreateBuilder<MetadataReference>();
        npgsqlReferences.AddRange(trustedPlatformAssemblies);
        npgsqlReferences.Add(typeof(INpgsqlBulkModel).Assembly.GetReference());
        npgsqlReferences.Add(typeof(NpgsqlConnection).Assembly.GetReference());
        NpgsqlReferences = npgsqlReferences.ToImmutable();
    }
}
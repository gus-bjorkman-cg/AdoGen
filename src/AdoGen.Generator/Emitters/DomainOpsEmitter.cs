using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AdoGen.Generator.Diagnostics;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;
using AdoGen.Generator.Parsing;

namespace AdoGen.Generator.Emitters;

internal static class DomainOpsEmitter
{
    private const string AggressiveInlining = "[MethodImpl(MethodImplOptions.AggressiveInlining)]";

    public static void Emit(
        SourceProductionContext spc,
        (((INamedTypeSymbol dto, bool _, bool missingInterface) domain, ImmutableArray<(INamedTypeSymbol Dto, INamedTypeSymbol Profile, SemanticModel Model)> profilesIndex) input, Compilation compilation) data)
    {
        var ((domain, profilesIndex), compilation) = data;
        var (dto, _, missingInterface) = domain;

        if (missingInterface)
        {
            spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.MissingDomainInterface, Location.None));
            return;
        }

        var isPartial = dto.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(t => t.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));
        if (!isPartial)
        {
            spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.NotPartial, dto.Locations.FirstOrDefault() ?? Location.None, dto.ToDisplayString()));
            return;
        }

        // Try to find SqlProfile<T> for this DTO from precomputed index
        var profileEntry = profilesIndex.FirstOrDefault(p => SymbolEqualityComparer.Default.Equals(p.Dto, dto));
        ProfileInfo info;
        if (profileEntry.Profile is null)
        {
            // Build defaults (dbo, pluralized name, Id key if present, default type mappings)
            info = BuildDefaultProfileInfo(dto);
        }
        else
        {
            var collected = ProfileInfoCollector.Collect(profileEntry.Profile, dto, profileEntry.Model, spc);

            if (collected.Keys.IsDefaultOrEmpty || collected.Keys.Length == 0)
            {
                spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.MissingKey, profileEntry.Profile.Locations.FirstOrDefault() ?? dto.Locations.FirstOrDefault() ?? Location.None, dto.Name));
            }
            info = collected;
        }

        EmitWithInfo(spc, dto, info);
    }

    private static ProfileInfo BuildDefaultProfileInfo(INamedTypeSymbol dto)
    {
        var props = dto.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
            .ToArray();

        var dict = new Dictionary<string, ParamConfig>(StringComparer.Ordinal);
        
        foreach (var p in props)
        {
            dict[p.Name] = new ParamConfig
            {
                PropertyName = p.Name,
                PropertyType = p.Type,
                ParameterName = "@" + p.Name,
                DbType = p.Type.MapDefaultSqlDbType()
            };
        }

        var idName = props.FirstOrDefault(p => string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase))?.Name;
        var keys = idName is null ? ImmutableArray<string>.Empty : [idName];

        return new ProfileInfo(
            Schema: "dbo",
            Table: dto.Name.PluralizeSimple(),
            Keys: keys,
            IdentityKeys: ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal),
            ParamsByProperty: dict.ToImmutableDictionary(StringComparer.Ordinal)
        );
    }

    private static void EmitWithInfo(SourceProductionContext spc, INamedTypeSymbol dto, ProfileInfo info)
    {
        var dtoProps = dto.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
            .ToArray();

        var ns = dto.ContainingNamespace.IsGlobalNamespace ? "GlobalNamespace" : dto.ContainingNamespace.ToDisplayString();
        var dtoTypeName = dto.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var typeKeyword = dto.IsRecord ? "record" : "class";

        // CREATE TABLE
        var sbColDefs = new StringBuilder();
        for (var i = 0; i < dtoProps.Length; i++)
        {
            var p = dtoProps[i];
            var cfg = info.ParamsByProperty[p.Name];
            var sqlType = SqlTypeLiterals.ToSqlTypeLiteral(cfg);
            var isNullable = p.IsNullableProperty(cfg);
            var nullability = isNullable ? "NULL" : "NOT NULL";
            var identity = info.IdentityKeys.Contains(p.Name) ? " IDENTITY(1,1)" : "";
            var defaultSql = p.ResolveDefaultSql(cfg);
            var defaultClause = defaultSql is not null ? $" {defaultSql}" : "";

            const string spaces = "            ";
            var comma = i == dtoProps.Length - 1 ? "" : ",";
            var line = $"{spaces}[{p.Name}] {sqlType}{identity}{defaultClause} {nullability}{comma}";
            sbColDefs.AppendLine(line);
        }

        if (info.Keys.Length > 0)
            sbColDefs.AppendLine($"        ,CONSTRAINT [PK_{info.Table}] PRIMARY KEY ({string.Join(", ", info.Keys.Select(k => $"[{k}]"))})");

        var colDefs = sbColDefs.ToString().TrimEnd();
        var createTableSql = 
            $"""
            CREATE TABLE [{info.Schema}].[{info.Table}](
            {colDefs});
            """;

        // INSERT (skip identity)
        var insertCols = dtoProps.Where(p => !info.IdentityKeys.Contains(p.Name)).Select(p => $"[{p.Name}]").ToArray();
        var insertParams = dtoProps.Where(p => !info.IdentityKeys.Contains(p.Name)).Select(p => "@" + p.Name).ToArray();
        var insertSql = $"INSERT INTO [{info.Schema}].[{info.Table}] ({string.Join(", ", insertCols)}) VALUES ({string.Join(", ", insertParams)});";

        // UPDATE (non-key, non-identity)
        var nonKeyNonIdentity = dtoProps.Where(p => !info.Keys.Contains(p.Name) && !info.IdentityKeys.Contains(p.Name)).ToArray();
        var updateSet = string.Join(", ", nonKeyNonIdentity.Select(p => $"[{p.Name}] = @{p.Name}"));
        var whereClause = string.Join(" AND ", info.Keys.Select(k => $"[{k}] = @{k}"));
        var updateSql = $"UPDATE [{info.Schema}].[{info.Table}] SET {updateSet} WHERE {whereClause};";
        var deleteSql = $"DELETE FROM [{info.Schema}].[{info.Table}] WHERE {whereClause};";

        // UPSERT via MERGE
        var matchKeys = info.Keys.Where(k => !info.IdentityKeys.Contains(k)).Select(k => $"T.[{k}] = S.[{k}]");
        var allCols = dtoProps.Select(p => $"[{p.Name}]").ToArray();
        var allParams = dtoProps.Select(p => "@" + p.Name).ToArray();
        var usingColumns = string.Join(", ", allCols);
        var usingValues  = string.Join(", ", allParams);
        var onExpr = string.Join(" AND ", matchKeys);
        var updateSetFromS = string.Join(", ", dtoProps.Where(p => !info.Keys.Contains(p.Name)).Select(p => $"T.[{p.Name}] = S.[{p.Name}]"));

        var insertCols2 = allCols.Where(c => !info.IdentityKeys.Contains(c.Trim('[', ']'))).ToArray();
        var insertValues2 = insertCols2.Select(c => $"S.{c}").ToArray();

        var upsertSql =
            $"""
             MERGE [{info.Schema}].[{info.Table}] AS T
                        USING (VALUES({usingValues})) AS S({usingColumns})
                        ON ({onExpr})
                        WHEN MATCHED THEN UPDATE SET {updateSetFromS}
                        WHEN NOT MATCHED THEN INSERT ({string.Join(", ", insertCols2)}) VALUES ({string.Join(", ", insertValues2)});
             """;

        var truncateSql = $"TRUNCATE TABLE [{info.Schema}].[{info.Table}];";

        string ParamAdd(string varName)
        {
            var sb = new StringBuilder();
            foreach (var p in dtoProps)
                sb.AppendLine($"        cmd.Parameters.Add({dto.Name}Sql.CreateParameter{p.Name}({varName}.{p.Name}));");
            return sb.ToString();
        }

        string ParamAddForUpdate(string varName)
        {
            var sb = new StringBuilder();
            foreach (var p in nonKeyNonIdentity)
                sb.AppendLine($"        cmd.Parameters.Add({dto.Name}Sql.CreateParameter{p.Name}({varName}.{p.Name}));");
            foreach (var k in info.Keys)
                sb.AppendLine($"        cmd.Parameters.Add({dto.Name}Sql.CreateParameter{k}({varName}.{k}));");
            return sb.ToString();
        }

        string ParamAddForDelete(string varName)
        {
            var sb = new StringBuilder();
            foreach (var k in info.Keys)
                sb.AppendLine($"        cmd.Parameters.Add({dto.Name}Sql.CreateParameter{k}({varName}.{k}));");
            return sb.ToString();
        }

        var src = $$""""
            // <auto-generated />
            #nullable enable
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;
            using System.Data;
            using Microsoft.Data.SqlClient;
            using AdoGen.Abstractions;

            namespace {{ns}};

            public sealed partial {{typeKeyword}} {{dto.Name}} : ISqlDomainModel<{{dtoTypeName}}>
            {
                private const string SqlCreateTable = 
                    """
                    {{createTableSql}}
                    """;
                private const string SqlInsert = "{{insertSql}}";
                private const string SqlUpdate = "{{updateSql}}";
                private const string SqlDelete = "{{deleteSql}}";
                private const string SqlTruncate = "{{truncateSql}}";
                private const string SqlUpsert = 
                    """
                    {{upsertSql}}
                    """;

                {{AggressiveInlining}}
                public static async ValueTask CreateTable(SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null)
                {
                    await using var cmd = connection.CreateCommand(SqlCreateTable, CommandType.Text, transaction);
                    await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                {{AggressiveInlining}}
                public static async ValueTask Insert({{dtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null)
                {
                    await using var cmd = connection.CreateCommand(SqlInsert, CommandType.Text, transaction);
            {{ParamAdd("model")}}
                    await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                // TODO: optimize with batch queries
                {{AggressiveInlining}}
                public static async ValueTask Insert(System.Collections.Generic.List<{{dtoTypeName}}> models, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null)
                {
                    if (models is null || models.Count == 0) return;
                    foreach (var m in models)
                    {
                        await using var cmd = connection.CreateCommand(SqlInsert, CommandType.Text, transaction);
            {{ParamAdd("m")}}
                        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                    }
                }

                {{AggressiveInlining}}
                public static async ValueTask Update({{dtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null)
                {
                    await using var cmd = connection.CreateCommand(SqlUpdate, CommandType.Text, transaction);
            {{ParamAddForUpdate("model")}}
                    await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                {{AggressiveInlining}}
                public static async ValueTask Delete({{dtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null)
                {
                    await using var cmd = connection.CreateCommand(SqlDelete, CommandType.Text, transaction);
            {{ParamAddForDelete("model")}}
                    await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                {{AggressiveInlining}}
                public static async ValueTask Upsert({{dtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null)
                {
                    await using var cmd = connection.CreateCommand(SqlUpsert, CommandType.Text, transaction);
            {{ParamAdd("model")}}
                    await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                {{AggressiveInlining}}
                public static async ValueTask Truncate(SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null)
                {
                    await using var cmd = connection.CreateCommand(SqlTruncate, CommandType.Text, transaction);
                    await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }
            }
            """";

        if (upsertSql is null)
        {
            spc.ReportDiagnostic(Diagnostic.Create(SqlDiagnostics.NoUpsertMatchKeys, dto.Locations.FirstOrDefault() ?? Location.None, dto.Name));
        }

        spc.AddSource($"{dto.Name}DomainOps.g.cs", src);
    }
}

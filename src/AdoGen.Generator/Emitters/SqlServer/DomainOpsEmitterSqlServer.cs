using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using AdoGen.Generator.Models;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Emitters.SqlServer;

internal sealed class DomainOpsEmitterSqlServer : IEmitter
{
    private DomainOpsEmitterSqlServer() {}
    public static DomainOpsEmitterSqlServer Instance { get; } = new();

    public bool IsMatch(SqlModelKind kind, SqlProviderKind provider) => 
        provider is SqlProviderKind.SqlServer && kind >= SqlModelKind.Domain;

    public void Handle(SourceProductionContext spc, ValidatedDiscoveryDto validatedDto, EmitContext ctx)
    {
        var (discoveryDto, profileInfo, _) = validatedDto;
        var dto = discoveryDto.Dto;

        // CREATE TABLE
        var sbColDefs = new StringBuilder();
        for (var i = 0; i < ctx.Columns.Length; i++)
        {
            var col = ctx.Columns[i];
            var nullability = col.IsNullable ? "NULL" : "NOT NULL";
            var identity = col.IsIdentity ? " IDENTITY(1,1)" : "";
            var defaultClause = col.DefaultSqlExpression is not null ? $" {col.DefaultSqlExpression}" : "";

            const string spaces = "            ";
            var comma = i == ctx.Columns.Length - 1 ? "" : ",";
            sbColDefs.AppendLine($"{spaces}{col.ColumnNameQuoted} {col.SqlType}{identity}{defaultClause} {nullability}{comma}");
        }

        if (ctx.Keys.Length > 0)
        {
            var pkCols = BuildJoined(ctx.Keys, col => col.ColumnNameQuoted);
            sbColDefs.AppendLine($"        ,CONSTRAINT [PK_{profileInfo.Table}] PRIMARY KEY ({pkCols})");
        }

        var colDefs = sbColDefs.ToString().TrimEnd();
        var createTableSql = 
            $"""
            CREATE TABLE {ctx.SchemaTableQuoted}(
            {colDefs});
            """;

        // INSERT (skip identity) — use pre-computed NonIdentities subset
        var insertCols = BuildJoined(ctx.NonIdentities, col => col.ColumnNameQuoted);
        var insertParams = BuildJoined(ctx.NonIdentities, col => "@" + col.ParameterName);
        var nonIdentityPropCount = ctx.NonIdentities.Length;

        var insertSql =
            $"INSERT INTO {ctx.SchemaTableQuoted} ({insertCols}) VALUES ({insertParams});";
        var insertBatchSql = $"INSERT INTO {ctx.SchemaTableQuoted} ({insertCols}) VALUES";

        // UPDATE (non-key, non-identity)
        var updateSet = BuildJoined(ctx.NonKeyNonIdentities, col => $"{col.ColumnNameQuoted} = @{col.ParameterName}");
        var updateSql = $"UPDATE {ctx.SchemaTableQuoted} SET {updateSet} WHERE {ctx.WhereByKey};";
        var deleteSql = $"DELETE FROM {ctx.SchemaTableQuoted} WHERE {ctx.WhereByKey};";

        // UPSERT via MERGE — ON clause uses non-identity keys only (matching original behavior)
        var usingColumns = BuildJoined(ctx.Columns, col => col.ColumnNameQuoted);
        var usingValues = BuildJoined(ctx.Columns, col => "@" + col.ParameterName);
        var nonIdentityKeys = ctx.Keys.Where(col => !col.IsIdentity).ToArray();
        var onExpr = BuildJoined(ImmutableArray.Create(nonIdentityKeys),
            col => $"T.{col.ColumnNameQuoted} = S.{col.ColumnNameQuoted}",
            separator: " AND ");

        var updateSetFromS = BuildJoined(ctx.NonKeyNonIdentities, col => $"T.{col.ColumnNameQuoted} = S.{col.ColumnNameQuoted}");
        var insertCols2 = BuildJoined(ctx.NonIdentities, col => col.ColumnNameQuoted);
        var insertValues2 = BuildJoined(ctx.NonIdentities, col => $"S.{col.ColumnNameQuoted}");

        var upsertSql =
            $"""
             MERGE {ctx.SchemaTableQuoted} AS T
                        USING (VALUES({usingValues})) AS S({usingColumns})
                        ON ({onExpr})
                        WHEN MATCHED THEN UPDATE SET {updateSetFromS}
                        WHEN NOT MATCHED THEN INSERT ({insertCols2}) VALUES ({insertValues2});
             """;

        var deleteSrc = "";
        if (profileInfo.Keys.Length == 1)
        {
            var keyName = profileInfo.Keys[0];
            var keyType = profileInfo.ParamsByProperty[keyName].PropertyType
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var deleteBatchSql = $"DELETE FROM {ctx.SchemaTableQuoted} WHERE [{keyName}] IN (";
            
            deleteSrc =
                $$""""
                  
                  {{ctx.Accessibility}} sealed partial {{ctx.TypeKeyword}} {{dto.Name}} : ISqlSingleIdModel<{{ctx.DtoTypeName}}, {{keyType}}>
                  {
                      private const string SqlDeleteBatchTemplate = "{{deleteBatchSql}}";
                  
                      public static async ValueTask<int> DeleteAsync(SqlConnection connection, List<{{keyType}}> ids, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                      {
                          if (ids is null || ids.Count == 0) return 0;
                          if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                          
                          var sb = new StringBuilder(SqlDeleteBatchTemplate);
                          for (var i = 0; i < ids.Count; i++)
                          {
                              if (i > 0) sb.Append(',');
                              sb.Append($"@p{i}");
                          }
                          sb.Append(')');
                          
                          await using var cmd = connection.CreateCommand(sb.ToString(), CommandType.Text, transaction, commandTimeout);
                          
                          for (var i = 0; i < ids.Count; i++)
                          {
                              cmd.Parameters.Add({{dto.Name}}Sql.CreateParameter{{keyName}}(ids[i], $"@p{i}"));
                          }
                          
                          return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                      }
                  }
                  
                  """";
        }

        var truncateSql = $"TRUNCATE TABLE {ctx.SchemaTableQuoted};";
        
        var src = 
            $$""""
            // <auto-generated />
            #nullable enable
            using System;
            using System.Data;
            using System.Text;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Data.SqlClient;
            using AdoGen.SqlServer;

            namespace {{ctx.Namespace}};
            {{deleteSrc}}
            {{ctx.Accessibility}} sealed partial {{ctx.TypeKeyword}} {{dto.Name}} : ISqlDomainModel<{{ctx.DtoTypeName}}>
            {
                private const string SqlCreateTable = 
                    """
                    {{createTableSql}}
                    """;
                private const string SqlInsert = "{{insertSql}}";
                private const string SqlInsertBatchTemplate = "{{insertBatchSql}}";
                private const string SqlUpdate = "{{updateSql}}";
                private const string SqlDelete = "{{deleteSql}}";
                private const string SqlTruncate = "{{truncateSql}}";
                private const string SqlUpsert = 
                    """
                    {{upsertSql}}
                    """;
            
                private const int NonIdentityPropertyCount = {{nonIdentityPropCount}};

                public static async ValueTask CreateTableAsync(SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                    await using var cmd = connection.CreateCommand(SqlCreateTable, CommandType.Text, transaction, commandTimeout);
                    await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                public static async ValueTask<int> InsertAsync({{ctx.DtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                    await using var cmd = connection.CreateCommand(SqlInsert, CommandType.Text, transaction, commandTimeout);
            {{ParamAdd("model")}}
                    return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                /// <summary>
                /// Inserts multiple database records in one roundtrip. 
                /// Will throw if parameter count exceeds SQL Server limit (2100).
                /// For type {{ctx.DtoTypeName}}, each record will use {{nonIdentityPropCount}} parameters.
                /// Resulting in a max insert count of {{2100 / nonIdentityPropCount}} per batch.
                /// For larger inserts, consider using SqlBulkCopy or multiple batches.
                /// </summary>
                /// <param name="models"></param>
                /// <param name="connection"></param>
                /// <param name="ct"></param>
                /// <param name="transaction"></param>
                /// <param name="commandTimeout"></param>
                /// <returns>Number of affected rows</returns>
                public static async ValueTask<int> InsertAsync(List<{{ctx.DtoTypeName}}> models, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (models is null || models.Count == 0) return 0;
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                    
                    var sb = new StringBuilder(SqlInsertBatchTemplate);
                    var paramIndex = 0;
                
                    for (var modelIndex = 0; modelIndex < models.Count; modelIndex++)
                    {
                        if (modelIndex > 0) sb.Append(',');
                
                        sb.Append('(');
                
                        for (var columnIndex = 0; columnIndex < NonIdentityPropertyCount; columnIndex++)
                        {
                            if (columnIndex > 0) sb.Append(',');
                            sb.Append($"@p{paramIndex + columnIndex}");
                        }
                
                        sb.Append(')');
                        paramIndex += NonIdentityPropertyCount;
                    }
                
                    await using var cmd = connection.CreateCommand(sb.ToString(), CommandType.Text, transaction, commandTimeout);
                    cmd.EnableOptimizedParameterBinding = (models.Count * NonIdentityPropertyCount) > 24;
                    paramIndex = 0;
                
                    foreach (var model in models)
                    {
            {{ParamAddBatchFlat("model", "paramIndex")}}
                    }
            
                    return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                public static async ValueTask<int> UpdateAsync({{ctx.DtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                    await using var cmd = connection.CreateCommand(SqlUpdate, CommandType.Text, transaction, commandTimeout);
            {{ParamAddForUpdate("model")}}
                    return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                public static async ValueTask<int> DeleteAsync({{ctx.DtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                    await using var cmd = connection.CreateCommand(SqlDelete, CommandType.Text, transaction, commandTimeout);
            {{ParamAddForDelete("model")}}
                    return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                public static async ValueTask<int> UpsertAsync({{ctx.DtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                    await using var cmd = connection.CreateCommand(SqlUpsert, CommandType.Text, transaction, commandTimeout);
            {{ParamAdd("model")}}
                    return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                public static async ValueTask<int> TruncateAsync(SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                    await using var cmd = connection.CreateCommand(SqlTruncate, CommandType.Text, transaction, commandTimeout);
                    return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }
            }
            """";

        // upsertSql is always non-null here (we validate conflict keys earlier). Remove dead check.

        spc.AddSource($"{dto.Name}.Domain.Sql.g.cs", src);
        
        return;
        
        string ParamAdd(string modelName)
        {
            var sb = new StringBuilder();
            foreach (var col in ctx.Columns)
                sb.AppendLine($"        cmd.Parameters.Add({dto.Name}Sql.CreateParameter{col.Name}({modelName}.{col.Name}));");
            return sb.ToString();
        }

        string ParamAddForUpdate(string modelName)
        {
            var sb = new StringBuilder();
            foreach (var col in ctx.NonKeyNonIdentities)
                sb.AppendLine($"        cmd.Parameters.Add({dto.Name}Sql.CreateParameter{col.Name}({modelName}.{col.Name}));");
            foreach (var col in ctx.Keys)
                sb.AppendLine($"        cmd.Parameters.Add({dto.Name}Sql.CreateParameter{col.Name}({modelName}.{col.Name}));");
            return sb.ToString();
        }

        string ParamAddForDelete(string modelName)
        {
            var sb = new StringBuilder();
            foreach (var col in ctx.Keys)
                sb.AppendLine($"        cmd.Parameters.Add({dto.Name}Sql.CreateParameter{col.Name}({modelName}.{col.Name}));");
            return sb.ToString();
        }
        
        string ParamAddBatchFlat(string modelName, string indexName)
        {
            var sb = new StringBuilder();
            foreach (var col in ctx.NonIdentities)
            {
                sb.AppendLine($"            cmd.Parameters.Add({dto.Name}Sql.CreateParameter{col.Name}({modelName}.{col.Name}, $\"@p{{{indexName}}}\"));");
                sb.AppendLine($"            {indexName}++;");
            }
            return sb.ToString().TrimEnd();
        }
    }

    private static string BuildJoined(ImmutableArray<ColumnInfo> columns, Func<ColumnInfo, string> selector, string separator = ", ")
    {
        if (columns.Length == 0) return string.Empty;
        if (columns.Length == 1) return selector(columns[0]);
        var sb = new StringBuilder(capacity: columns.Length * 24);
        for (var i = 0; i < columns.Length; i++)
        {
            if (i > 0) sb.Append(separator);
            sb.Append(selector(columns[i]));
        }
        return sb.ToString();
    }
}
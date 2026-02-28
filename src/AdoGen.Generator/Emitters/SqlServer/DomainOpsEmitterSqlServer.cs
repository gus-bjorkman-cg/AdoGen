using System.Linq;
using System.Text;
using AdoGen.Generator.Extensions;
using AdoGen.Generator.Models;
using AdoGen.Generator.Pipelines;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Emitters.SqlServer;

internal sealed class DomainOpsEmitterSqlServer : IEmitter
{
    private DomainOpsEmitterSqlServer() {}
    public static DomainOpsEmitterSqlServer Instance { get; } = new();

    public bool IsMatch(SqlModelKind kind, SqlProviderKind provider) => 
        provider is SqlProviderKind.SqlServer && kind >= SqlModelKind.Domain;

    public void Handle(SourceProductionContext spc, ValidatedDiscoveryDto validatedDto)
    {
        var (discoveryDto, profileInfo, _) = validatedDto;
        var dto = discoveryDto.Dto;
        var dtoProps = profileInfo.DtoProperties;

        var ns = profileInfo.Namespace;
        var dtoTypeName = dto.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var typeKeyword = dto.IsRecord ? "record" : "class";

        // CREATE TABLE
        var sbColDefs = new StringBuilder();
        for (var i = 0; i < dtoProps.Length; i++)
        {
            var p = dtoProps[i];
            var cfg = profileInfo.ParamsByProperty[p.Name];
            var sqlType = cfg.SqlTypeLiteral;
            var isNullable = p.IsNullableProperty(cfg);
            var nullability = isNullable ? "NULL" : "NOT NULL";
            var identity = profileInfo.IdentityKeys.Contains(p.Name) ? " IDENTITY(1,1)" : "";
            var defaultSql = p.ResolveDefaultSql(cfg);
            var defaultClause = defaultSql is not null ? $" {defaultSql}" : "";

            const string spaces = "            ";
            var comma = i == dtoProps.Length - 1 ? "" : ",";
            var line = $"{spaces}[{cfg.ParameterName}] {sqlType}{identity}{defaultClause} {nullability}{comma}";
            sbColDefs.AppendLine(line);
        }

        if (profileInfo.Keys.Length > 0)
            sbColDefs.AppendLine($"        ,CONSTRAINT [PK_{profileInfo.Table}] PRIMARY KEY ({string.Join(", ", profileInfo.Keys.Select(k => $"[{k}]"))})");

        var colDefs = sbColDefs.ToString().TrimEnd();
        var createTableSql = 
            $"""
            CREATE TABLE [{profileInfo.Schema}].[{profileInfo.Table}](
            {colDefs});
            """;

        // INSERT (skip identity)
        var insertCols = dtoProps
            .Where(p => !profileInfo.IdentityKeys.Contains(p.Name))
            .Select(p => $"[{profileInfo.ParamsByProperty[p.Name].ParameterName}]")
            .ToArray();

        var insertParams = dtoProps
            .Where(p => !profileInfo.IdentityKeys.Contains(p.Name))
            .Select(p => "@" + profileInfo.ParamsByProperty[p.Name].ParameterName)
            .ToArray();

        var insertSql =
            $"INSERT INTO [{profileInfo.Schema}].[{profileInfo.Table}] ({string.Join(", ", insertCols)}) VALUES ({string.Join(", ", insertParams)});";

        var insertBatchSql = $"INSERT INTO [{profileInfo.Schema}].[{profileInfo.Table}] ({string.Join(", ", insertCols)}) VALUES";

        // UPDATE (non-key, non-identity)
        var nonKeyNonIdentity = dtoProps
            .Where(p => !profileInfo.Keys.Contains(p.Name) && !profileInfo.IdentityKeys.Contains(p.Name))
            .ToArray();

        var updateSet = string.Join(", ", nonKeyNonIdentity.Select(p =>
        {
            var col = profileInfo.ParamsByProperty[p.Name].ParameterName;
            return $"[{col}] = @{col}";
        }));

        var whereClause = string.Join(" AND ", profileInfo.Keys.Select(k =>
        {
            var col = profileInfo.ParamsByProperty[k].ParameterName;
            return $"[{col}] = @{col}";
        }));

        var updateSql = $"UPDATE [{profileInfo.Schema}].[{profileInfo.Table}] SET {updateSet} WHERE {whereClause};";
        var deleteSql = $"DELETE FROM [{profileInfo.Schema}].[{profileInfo.Table}] WHERE {whereClause};";

        // UPSERT via MERGE
        var matchKeys = profileInfo.Keys
            .Where(k => !profileInfo.IdentityKeys.Contains(k))
            .Select(k =>
            {
                var col = profileInfo.ParamsByProperty[k].ParameterName;
                return $"T.[{col}] = S.[{col}]";
            });

        var allCols = dtoProps
            .Select(p => $"[{profileInfo.ParamsByProperty[p.Name].ParameterName}]")
            .ToArray();

        var allParams = dtoProps
            .Select(p => "@" + profileInfo.ParamsByProperty[p.Name].ParameterName)
            .ToArray();

        var usingColumns = string.Join(", ", allCols);
        var usingValues = string.Join(", ", allParams);
        var onExpr = string.Join(" AND ", matchKeys);

        var updateSetFromS = string.Join(", ", dtoProps
            .Where(p => !profileInfo.Keys.Contains(p.Name))
            .Select(p =>
            {
                var col = profileInfo.ParamsByProperty[p.Name].ParameterName;
                return $"T.[{col}] = S.[{col}]";
            }));

        var nonIdentityProp = dtoProps.Where(p => !profileInfo.IdentityKeys.Contains(p.Name)).ToArray();
        var nonIdentityPropCount = nonIdentityProp.Length;

        var insertCols2 = dtoProps
            .Where(p => !profileInfo.IdentityKeys.Contains(p.Name))
            .Select(p => $"[{profileInfo.ParamsByProperty[p.Name].ParameterName}]")
            .ToArray();

        var insertValues2 = insertCols2.Select(c => $"S.{c}").ToArray();

        var upsertSql =
            $"""
             MERGE [{profileInfo.Schema}].[{profileInfo.Table}] AS T
                        USING (VALUES({usingValues})) AS S({usingColumns})
                        ON ({onExpr})
                        WHEN MATCHED THEN UPDATE SET {updateSetFromS}
                        WHEN NOT MATCHED THEN INSERT ({string.Join(", ", insertCols2)}) VALUES ({string.Join(", ", insertValues2)});
             """;

        var deleteSrc = "";
        if (profileInfo.Keys.Length == 1)
        {
            var keyName = profileInfo.Keys[0];
            var keyType = profileInfo.ParamsByProperty[keyName].PropertyType
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var deleteBatchSql = $"DELETE FROM [{profileInfo.Schema}].[{profileInfo.Table}] WHERE [{keyName}] IN (";
            
            deleteSrc =
                $$""""
                  
                  public sealed partial {{typeKeyword}} {{dto.Name}} : ISqlSingleIdModel<{{dtoTypeName}}, {{keyType}}>
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
                              cmd.Parameters.Add(AdoGen.SqlServer.{{dto.Name}}Sql.CreateParameter{{keyName}}(ids[i], $"@p{i}"));
                          }
                          
                          return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                      }
                  }
                  
                  """";
        }

        var truncateSql = $"TRUNCATE TABLE [{profileInfo.Schema}].[{profileInfo.Table}];";
        
        var src = $$""""
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

            namespace {{ns}};
            {{deleteSrc}}
            public sealed partial {{typeKeyword}} {{dto.Name}} : ISqlDomainModel<{{dtoTypeName}}>
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

                public static async ValueTask<int> InsertAsync({{dtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                    await using var cmd = connection.CreateCommand(SqlInsert, CommandType.Text, transaction, commandTimeout);
            {{ParamAdd("model")}}
                    return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                /// <summary>
                /// Inserts multiple database records in one roundtrip. 
                /// Will throw if parameter count exceeds SQL Server limit (2100).
                /// For type {{dtoTypeName}}, each record will use {{nonIdentityPropCount}} parameters.
                /// Resulting in a max insert count of {{2100 / nonIdentityPropCount}} per batch.
                /// For larger inserts, consider using SqlBulkCopy or multiple batches.
                /// </summary>
                /// <param name="models"></param>
                /// <param name="connection"></param>
                /// <param name="ct"></param>
                /// <param name="transaction"></param>
                /// <param name="commandTimeout"></param>
                /// <returns>Number of affected rows</returns>
                public static async ValueTask<int> InsertAsync(List<{{dtoTypeName}}> models, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
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

                public static async ValueTask<int> UpdateAsync({{dtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                    await using var cmd = connection.CreateCommand(SqlUpdate, CommandType.Text, transaction, commandTimeout);
            {{ParamAddForUpdate("model")}}
                    return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                public static async ValueTask<int> DeleteAsync({{dtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                    await using var cmd = connection.CreateCommand(SqlDelete, CommandType.Text, transaction, commandTimeout);
            {{ParamAddForDelete("model")}}
                    return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                public static async ValueTask<int> UpsertAsync({{dtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
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

        spc.AddSource($"{dto.Name}DomainOps.Sql.g.cs", src);
        
        return;
        
        string ParamAdd(string modelName)
        {
            var sb = new StringBuilder();
            foreach (var p in dtoProps)
                sb.AppendLine($"        cmd.Parameters.Add({dto.Name}Sql.CreateParameter{p.Name}({modelName}.{p.Name}));");
            return sb.ToString();
        }

        string ParamAddForUpdate(string modelName)
        {
            var sb = new StringBuilder();
            foreach (var p in nonKeyNonIdentity)
                sb.AppendLine($"        cmd.Parameters.Add({dto.Name}Sql.CreateParameter{p.Name}({modelName}.{p.Name}));");
            foreach (var k in profileInfo.Keys)
                sb.AppendLine($"        cmd.Parameters.Add({dto.Name}Sql.CreateParameter{k}({modelName}.{k}));");
            return sb.ToString();
        }

        string ParamAddForDelete(string modelName)
        {
            var sb = new StringBuilder();
            foreach (var k in profileInfo.Keys)
                sb.AppendLine($"        cmd.Parameters.Add({dto.Name}Sql.CreateParameter{k}({modelName}.{k}));");
            return sb.ToString();
        }
        
        string ParamAddBatchFlat(string modelName, string indexName)
        {
            var sb = new StringBuilder();
            foreach (var p in nonIdentityProp)
            {
                sb.AppendLine($"            cmd.Parameters.Add({dto.Name}Sql.CreateParameter{p.Name}({modelName}.{p.Name}, $\"@p{{{indexName}}}\"));");
                sb.AppendLine($"            {indexName}++;");
            }
            return sb.ToString().TrimEnd();
        }
    }
}
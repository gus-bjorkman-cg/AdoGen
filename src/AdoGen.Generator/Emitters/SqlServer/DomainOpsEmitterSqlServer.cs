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

        // SQL strings — produced by SqlServerSqlTextBuilder
        var createTableSql = SqlServerSqlTextBuilder.CreateTable(ctx);
        var insertSql = SqlServerSqlTextBuilder.Insert(ctx);
        var insertBatchSql = SqlServerSqlTextBuilder.InsertBatchPrefix(ctx);
        var updateSql = SqlServerSqlTextBuilder.Update(ctx);
        var deleteSql = SqlServerSqlTextBuilder.Delete(ctx);
        var upsertSql = SqlServerSqlTextBuilder.Upsert(ctx);
        var nonIdentityPropCount = ctx.Writables.Length;
        
        var deleteSrc = "";
        
        if (profileInfo.Keys.Length == 1)
        {
            // Single-key: implement ISqlSingleIdModel<TModel, TKey>
            // Caller uses: connection.DeleteAsync<User>(ids, ct)  — no model object needed
            var keyName = profileInfo.Keys[0];
            var keyType = profileInfo.ParamsByProperty[keyName].PropertyType
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var deleteBatchPrefix = SqlServerSqlTextBuilder.DeleteBatchJoinValuesPrefix(ctx);
            var deleteBatchSuffix = SqlServerSqlTextBuilder.DeleteBatchJoinValuesSuffix(ctx);
            
            deleteSrc =
                $$""""
                  
                  {{ctx.Accessibility}} sealed partial {{ctx.TypeKeyword}} {{dto.Name}} : ISqlSingleIdModel<{{ctx.DtoTypeName}}, {{keyType}}>
                  {
                      private const string SqlDeleteBatchPrefix = "{{deleteBatchPrefix}}";
                      private const string SqlDeleteBatchSuffix = "{{deleteBatchSuffix}}";
                  
                      public static async ValueTask<int> DeleteAsync(SqlConnection connection, List<{{keyType}}> ids, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                      {
                          if (ids is null || ids.Count == 0) return 0;
                          if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                          
                          await using var cmd = new SqlCommand("", connection, transaction);
                          if (commandTimeout.HasValue) cmd.CommandTimeout = commandTimeout.Value;
                          cmd.EnableOptimizedParameterBinding = ids.Count > 24;
                          
                          var sb = new StringBuilder(SqlDeleteBatchPrefix);
                          for (var i = 0; i < ids.Count; i++)
                          {
                              if (i > 0) sb.Append(',');
                              sb.Append($"(@p{i})");
                              cmd.Parameters.Add({{dto.Name}}Sql.CreateParameter{{keyName}}(ids[i], $"@p{i}"));
                          }
                          sb.Append(SqlDeleteBatchSuffix);
                          cmd.CommandText = sb.ToString();
                          
                          return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                      }
                  }
                  
                  """";
        }
        else if (profileInfo.Keys.Length > 1)
        {
            // Composite-key: implement ISqlCompositeKeyModel<TModel>
            // Caller uses: connection.DeleteAsync(models, ct)
            var keyCount = ctx.Keys.Length;
            var deleteBatchPrefix = SqlServerSqlTextBuilder.DeleteBatchJoinValuesPrefix(ctx);
            var deleteBatchSuffix = SqlServerSqlTextBuilder.DeleteBatchJoinValuesSuffix(ctx);
            
            deleteSrc =
                $$""""
                  
                  {{ctx.Accessibility}} sealed partial {{ctx.TypeKeyword}} {{dto.Name}} : ISqlCompositeKeyModel<{{ctx.DtoTypeName}}>
                  {
                      private const string SqlDeleteBatchPrefix = "{{deleteBatchPrefix}}";
                      private const string SqlDeleteBatchSuffix = "{{deleteBatchSuffix}}";
                      private const int KeyPropertyCount = {{keyCount}};
                  
                      public static async ValueTask<int> DeleteAsync(List<{{ctx.DtoTypeName}}> models, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                      {
                          if (models is null || models.Count == 0) return 0;
                          if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                          
                          await using var cmd = new SqlCommand("", connection, transaction);
                          if (commandTimeout.HasValue) cmd.CommandTimeout = commandTimeout.Value;
                          cmd.EnableOptimizedParameterBinding = models.Count * KeyPropertyCount > 24;
                          
                          var sb = new StringBuilder(SqlDeleteBatchPrefix);
                          var paramIndex = 0;
                          
                          for (var i = 0; i < models.Count; i++)
                          {
                              if (i > 0) sb.Append(',');
                  {{ParameterBindingEmitter.BindKeysInlineLoop(ctx, "models[i]", "paramIndex", "sb", 12)}}
                          }
                          sb.Append(SqlDeleteBatchSuffix);
                          cmd.CommandText = sb.ToString();
                          
                          return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                      }
                  }
                  
                  """";
        }

        var truncateSql = SqlServerSqlTextBuilder.Truncate(ctx);
        
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
                private const string SqlUpsert = "{{upsertSql}}";
                private const string SqlTruncate = "{{truncateSql}}";

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
            {{ParameterBindingEmitter.BindAll(ctx, "model", 8)}}
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

                    await using var cmd = new SqlCommand("", connection, transaction);
                    if (commandTimeout.HasValue) cmd.CommandTimeout = commandTimeout.Value;
                    cmd.EnableOptimizedParameterBinding = (models.Count * {{nonIdentityPropCount}}) > 24;

                    var sb = new StringBuilder(SqlInsertBatchTemplate.Length + models.Count * {{ParameterBindingEmitter.BatchInsertPerRowEstimate(ctx)}});
                    sb.Append(SqlInsertBatchTemplate);

                    var paramIndex = 0;

                    for (var modelIndex = 0; modelIndex < models.Count; modelIndex++)
                    {
                        if (modelIndex > 0) sb.Append(',');
            {{ParameterBindingEmitter.BindWritablesInlineLoop(ctx, "models[modelIndex]", "paramIndex", "sb", 12)}}
                    }

                    cmd.CommandText = sb.ToString();
                    return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                public static async ValueTask<int> UpdateAsync({{ctx.DtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                    await using var cmd = connection.CreateCommand(SqlUpdate, CommandType.Text, transaction, commandTimeout);
            {{ParameterBindingEmitter.BindForUpdate(ctx, "model", 8)}}
                    return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                public static async ValueTask<int> DeleteAsync({{ctx.DtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                    await using var cmd = connection.CreateCommand(SqlDelete, CommandType.Text, transaction, commandTimeout);
            {{ParameterBindingEmitter.BindForDelete(ctx, "model", 8)}}
                    return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                public static async ValueTask<int> UpsertAsync({{ctx.DtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                    await using var cmd = connection.CreateCommand(SqlUpsert, CommandType.Text, transaction, commandTimeout);
            {{ParameterBindingEmitter.BindForUpsertSqlServer(ctx, "model", 8)}}
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
        
        spc.AddSource($"{dto.Name}.Domain.Sql.g.cs", src);
    }
}
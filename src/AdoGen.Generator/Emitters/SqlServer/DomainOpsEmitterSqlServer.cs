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

        // SQL strings — produced by SqlServerSqlTextBuilder
        var createTableSql = SqlServerSqlTextBuilder.CreateTable(ctx);
        var insertSql = SqlServerSqlTextBuilder.Insert(ctx);
        var insertBatchSql = SqlServerSqlTextBuilder.InsertBatchPrefix(ctx);
        var insertAndReturnSql = SqlServerSqlTextBuilder.InsertAndReturn(ctx);
        var updateSql = SqlServerSqlTextBuilder.Update(ctx);
        var deleteSql = SqlServerSqlTextBuilder.Delete(ctx);
        var upsertSql = SqlServerSqlTextBuilder.Upsert(ctx);
        var existsSql = SqlServerSqlTextBuilder.Exists(ctx);
        
        var nonIdentityPropCount = ctx.Writables.Length;
        var idBasedSrc = "";
        
        
        if (profileInfo.Keys.Length == 1)
        {
            // Single-key: implement ISqlSingleIdModel<TModel, TKey>
            // Caller uses: connection.DeleteAsync<User>(ids, ct)  — no model object needed
            var keyName = profileInfo.Keys[0];
            var keyType = profileInfo.ParamsByProperty[keyName].PropertyType
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var deleteBatchPrefix = SqlServerSqlTextBuilder.DeleteBatchJoinValuesPrefix(ctx);
            var deleteBatchSuffix = SqlServerSqlTextBuilder.DeleteBatchJoinValuesSuffix(ctx);
            
            idBasedSrc =
                $$""""
                  
                  {{ctx.Accessibility}} sealed partial {{ctx.TypeKeyword}} {{dto.Name}} : ISqlSingleIdModel<{{ctx.DtoTypeName}}, {{keyType}}>
                  {
                      private const string SqlDeleteBatchPrefix = "{{deleteBatchPrefix}}";
                      private const string SqlDeleteBatchSuffix = "{{deleteBatchSuffix}}";
                      private const string SqlExists = "{{existsSql}}";
                  
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
                  
                      public static async ValueTask<bool> ExistsAsync(SqlConnection connection, {{keyType}} id, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                      {
                          if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                          
                          await using var cmd = connection.CreateCommand(SqlExists, CommandType.Text, transaction, commandTimeout);
                          
                          cmd.Parameters.Add({{dto.Name}}Sql.CreateParameter{{keyName}}(id));
                          var scalar = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                          
                          return scalar is not null && scalar is not DBNull;
                      }
                  }
                  
                  """";
        }
        else if (profileInfo.Keys.Length > 1)
        {
            // Composite-key: implement ISqlCompositeKeyModel<TModel> and ISqlCompositeKeyExistsModel<TModel>
            // Caller uses: connection.DeleteAsync(models, ct) / connection.ExistsAsync(model, ct)
            var keyCount = ctx.Keys.Length;
            var deleteBatchPrefix = SqlServerSqlTextBuilder.DeleteBatchJoinValuesPrefix(ctx);
            var deleteBatchSuffix = SqlServerSqlTextBuilder.DeleteBatchJoinValuesSuffix(ctx);
            
            idBasedSrc =
                $$""""
                  
                  {{ctx.Accessibility}} sealed partial {{ctx.TypeKeyword}} {{dto.Name}} : ISqlCompositeKeyModel<{{ctx.DtoTypeName}}>, ISqlCompositeKeyExistsModel<{{ctx.DtoTypeName}}>
                  {
                      private const string SqlDeleteBatchPrefix = "{{deleteBatchPrefix}}";
                      private const string SqlDeleteBatchSuffix = "{{deleteBatchSuffix}}";
                      private const string SqlExists = "{{existsSql}}";
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
                  
                      public static async ValueTask<bool> ExistsAsync({{ctx.DtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                      {
                          if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                          await using var cmd = connection.CreateCommand(SqlExists, CommandType.Text, transaction, commandTimeout);
                  {{ParameterBindingEmitter.BindKeys(ctx, "model", 8)}}
                  
                          var scalar = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                          
                          return scalar is not null && scalar is not DBNull;
                      }
                  }
                  
                  """";
        }

        var truncateSql = SqlServerSqlTextBuilder.Truncate(ctx);

        // Update/Delete/Upsert method bodies — vary based on whether a concurrency token is configured
        var updateBody = ctx.ConcurrencyToken is not null
            ? $$"""
                      var affected = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                              if (affected == 0) throw new global::AdoGen.SqlServer.AdoGenConcurrencyException("{{ctx.Profile.Schema}}.{{ctx.Profile.Table}}");
                              return affected;
              """
            : "        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);";

        var deleteBody = ctx.ConcurrencyToken is not null
            ? $$"""
                      var affected = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                              if (affected == 0) throw new global::AdoGen.SqlServer.AdoGenConcurrencyException("{{ctx.Profile.Schema}}.{{ctx.Profile.Table}}");
                              return affected;
              """
            : "        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);";

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
            {{idBasedSrc}}
            {{ctx.Accessibility}} sealed partial {{ctx.TypeKeyword}} {{dto.Name}} : ISqlDomainModel<{{ctx.DtoTypeName}}>
            {
                private const string SqlCreateTable = 
                    """
                    {{createTableSql}}
                    """;
                private const string SqlInsert = "{{insertSql}}";
                private const string SqlInsertBatchTemplate = "{{insertBatchSql}}";
                private const string SqlInsertAndReturn = "{{insertAndReturnSql}}";
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
            {{updateBody}}
                }

                public static async ValueTask<{{ctx.DtoTypeName}}> InsertAndReturnAsync({{ctx.DtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                    
                    await using var cmd = connection.CreateCommand(SqlInsertAndReturn, CommandType.Text, transaction, commandTimeout);
                    
            {{ParameterBindingEmitter.BindAll(ctx, "model", 8)}}
                    await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, ct).ConfigureAwait(false);
                    
                    if (await reader.ReadAsync(ct).ConfigureAwait(false)) return {{dto.Name}}.Map(reader);
                    
                    throw new InvalidOperationException("InsertAndReturnAsync produced no row.");
                }

                public static async ValueTask<int> DeleteAsync({{ctx.DtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                    
                    await using var cmd = connection.CreateCommand(SqlDelete, CommandType.Text, transaction, commandTimeout);
                    
            {{ParameterBindingEmitter.BindForDelete(ctx, "model", 8)}}
            {{deleteBody}}
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
        
        var patchSrc = BuildPatchSrc(ctx, dto);
        
        spc.AddSource($"{dto.Name}.Domain.Sql.g.cs", patchSrc.Length == 0 ? src : src + "\n" + patchSrc);
    }

    private static string BuildPatchSrc(EmitContext ctx, INamedTypeSymbol dto)
    {
        var patchColumns = ctx.WritableNonKeyNonIdentities;
        // Skip if no patchable columns or too many (>64 exceeds bitmask capacity)
        if (patchColumns.Length is 0 or > 64) return string.Empty;

        var patchClassName = $"{dto.Name}Patch";

        // Key properties and constructor — supports composite keys
        var keyPropsBuilder = new StringBuilder();
        var ctorParamsBuilder = new StringBuilder();
        var ctorAssignBuilder = new StringBuilder();
        var keyBindBuilder = new StringBuilder();
        for (var k = 0; k < ctx.Keys.Length; k++)
        {
            var key = ctx.Keys[k];
            if (k > 0) ctorParamsBuilder.Append(", ");
            keyPropsBuilder.AppendLine($"    public {key.PropertyType} {key.Name} {{ get; }}");
            ctorParamsBuilder.Append($"{key.PropertyType} {LowerFirst(key.Name)}");
            ctorAssignBuilder.Append($"{key.Name} = {LowerFirst(key.Name)}; ");
            keyBindBuilder.AppendLine($"        cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{key.Name}(patch.{key.Name}));");
        }

        var publicPropsBuilder = new StringBuilder();
        var fluentBuilder = new StringBuilder();
        var valuePropsBuilder = new StringBuilder();
        for (var i = 0; i < patchColumns.Length; i++)
        {
            var col = patchColumns[i];
            var nullableType = $"{col.PropertyType.TrimEnd('?')}?";
            publicPropsBuilder.AppendLine($"    public {nullableType} {col.Name} {{ get; set {{ field = value; _mask |= 1UL << {i}; }} }}");
            fluentBuilder.AppendLine($"    public {patchClassName} With{col.Name}({col.PropertyType} value) {{ {col.Name} = value; return this; }}");
            valuePropsBuilder.AppendLine($"    internal {nullableType} {col.Name}Value => {col.Name};");
        }

        var concurrencyField = ctx.ConcurrencyToken is { } ct2
            ? $"    public {ct2.PropertyType} {ct2.Name} {{ get; set; }}\n" +
              $"    public {patchClassName} With{ct2.Name}({ct2.PropertyType} value) {{ {ct2.Name} = value; return this; }}\n" +
              $"    internal {ct2.PropertyType} {ct2.Name}Value => {ct2.Name};\n"
            : "";

        var setClauseItems = new StringBuilder();
        for (var i = 0; i < patchColumns.Length; i++)
        {
            var col = patchColumns[i];
            setClauseItems.AppendLine(
                $"        if ((patch.Mask & (1UL << {i})) != 0) {{ sb.Append({(i > 0 ? "separator + " : "")}\"[{col.ParameterName}] = @{col.ParameterName}\"); separator = \", \"; cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}(({col.PropertyType.TrimEnd('?')})patch.{col.Name}Value!)); }}");
        }

        var concurrencyWhere = ctx.ConcurrencyToken is { } token
            ? $"            cmd.CommandText += \" AND [{token.ParameterName}] = @{token.ParameterName}\";\n" +
              $"            cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{token.Name}(patch.{token.Name}Value));\n"
            : "";

        var concurrencyThrow = ctx.ConcurrencyToken is not null
            ? $$"""
                        var affected = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                        if (affected == 0) throw new global::AdoGen.SqlServer.AdoGenConcurrencyException("{{ctx.Profile.Schema}}.{{ctx.Profile.Table}}");
                        return affected;
              """
            : "        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);";

        var patchClassBody = ctx.ShouldGeneratePatchClass
            ? $$""""

                /// <summary>
                /// Patch command object for <see cref="{{dto.Name}}"/>.
                /// Set only the properties you want to update; unset properties are not written to the database.
                /// </summary>
                public partial class {{patchClassName}}
                {
                    private ulong _mask;
                {{keyPropsBuilder.ToString().TrimEnd()}}
                {{concurrencyField.TrimEnd()}}
                {{publicPropsBuilder.ToString().TrimEnd()}}
                {{fluentBuilder.ToString().TrimEnd()}}
                {{valuePropsBuilder.ToString().TrimEnd()}}
                    internal ulong Mask => _mask;

                    public {{patchClassName}}({{ctorParamsBuilder}}) { {{ctorAssignBuilder.ToString().TrimEnd()}} }
                }
                """"
            : string.Empty;

        return $$""""
            {{patchClassBody}}
            {{ctx.Accessibility}} sealed partial {{ctx.TypeKeyword}} {{dto.Name}}
            {
                public static async ValueTask<int> PatchAsync(SqlConnection connection, {{patchClassName}} patch, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (patch.Mask == 0UL) return 0;
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);

                    await using var cmd = new SqlCommand("", connection, transaction);
                    if (commandTimeout.HasValue) cmd.CommandTimeout = commandTimeout.Value;

                    var sb = new StringBuilder("UPDATE {{ctx.SchemaTableQuoted}} SET ");
                    var separator = "";
                    
            {{setClauseItems.ToString().TrimEnd()}}
            
                    sb.Append(" WHERE {{ctx.WhereByKey}}");
            {{concurrencyWhere.TrimEnd()}}
                    cmd.CommandText = sb.ToString();
            {{keyBindBuilder.ToString().TrimEnd()}}
                    
            {{concurrencyThrow}}
                }
            }

            /// <summary>Extension methods for patching <see cref="{{dto.Name}}"/> via <see cref="SqlConnection"/>.</summary>
            public static class {{dto.Name}}SqlPatchExtensions
            {
                /// <summary>
                /// Executes a partial UPDATE for <see cref="{{dto.Name}}"/>, touching only the columns
                /// explicitly set on <paramref name="patch"/>.
                /// Returns 0 immediately (without sending SQL) when no columns were set.
                /// </summary>
                public static global::System.Threading.Tasks.ValueTask<int> PatchAsync(
                    this SqlConnection connection,
                    {{patchClassName}} patch,
                    global::System.Threading.CancellationToken ct,
                    SqlTransaction? transaction = null,
                    int? commandTimeout = null)
                    => {{dto.Name}}.PatchAsync(connection, patch, ct, transaction, commandTimeout);
            }
            """";
    }

    private static string LowerFirst(string s)
    {
        if (s.Length == 0) return s;
        var result = char.ToLowerInvariant(s[0]) + s[1..];
        // Escape C# keywords used as parameter names
        return result switch
        {
            "int" or "long" or "short" or "byte" or "bool" or "string" or "float" or "double" or "decimal"
                or "object" or "char" or "void" or "class" or "interface" or "struct" or "enum"
                or "namespace" or "using" or "static" or "readonly" or "const" or "new" or "null"
                or "true" or "false" or "this" or "base" or "return" or "var" or "in" or "out" or "ref"
                => "@" + result,
            _ => result
        };
    }
}
using System;
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
        var createTableSql = SqlTextHelpers.IndentContinuationLines(SqlServerSqlTextBuilder.CreateTable(ctx), 8);
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
                  
                  {{ctx.Accessibility}} partial {{ctx.TypeKeyword}} {{dto.Name}} : ISqlSingleIdModel<{{ctx.DtoTypeName}}, {{keyType}}>
                  {
                      private const string SqlDeleteBatchPrefix = "{{deleteBatchPrefix}}";
                      private const string SqlDeleteBatchSuffix = "{{deleteBatchSuffix}}";
                      private const string SqlExists = "{{existsSql}}";
                  
                      public static async ValueTask<int> DeleteAsync(SqlConnection connection, List<{{keyType}}> ids, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                      {
                          if (ids is null || ids.Count == 0) return 0;
                          await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
                          
                          // SQL Server limits: 2100 parameters per command, 1000 rows per VALUES constructor.
                          const int RowsPerCommand = 1000;
                          var affected = 0;
                          
                          for (var start = 0; start < ids.Count; start += RowsPerCommand)
                          {
                              var end = Math.Min(start + RowsPerCommand, ids.Count);
                              // Pad to a power-of-two bucket by repeating the last id (harmless for DELETE),
                              // so the SQL text and its cached plan are shared across list sizes.
                              var slots = SqlParameterNames.BucketSize(end - start, RowsPerCommand);
                              
                              await using var cmd = new SqlCommand("", connection, transaction);
                              
                              if (commandTimeout.HasValue) cmd.CommandTimeout = commandTimeout.Value;
                              
                              cmd.EnableOptimizedParameterBinding = slots > 24;
                              
                              var sb = new StringBuilder(SqlDeleteBatchPrefix.Length + SqlDeleteBatchSuffix.Length + slots * 9);
                              sb.Append(SqlDeleteBatchPrefix);
                              
                              for (var p = 0; p < slots; p++)
                              {
                                  var name = SqlParameterNames.Get(p);
                                  if (p > 0) sb.Append(',');
                                  sb.Append('(').Append(name).Append(')');
                                  cmd.Parameters.Add({{dto.Name}}Sql.CreateParameter{{keyName}}(ids[Math.Min(start + p, end - 1)], name));
                              }
                              
                              sb.Append(SqlDeleteBatchSuffix);
                              cmd.CommandText = sb.ToString();
                              
                              affected += await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                          }
                          
                          return affected;
                      }
                  
                      public static async ValueTask<bool> ExistsAsync(SqlConnection connection, {{keyType}} id, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                      {
                          await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
                          
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
                  
                  {{ctx.Accessibility}} partial {{ctx.TypeKeyword}} {{dto.Name}} : ISqlCompositeKeyModel<{{ctx.DtoTypeName}}>, ISqlCompositeKeyExistsModel<{{ctx.DtoTypeName}}>
                  {
                      private const string SqlDeleteBatchPrefix = "{{deleteBatchPrefix}}";
                      private const string SqlDeleteBatchSuffix = "{{deleteBatchSuffix}}";
                      private const string SqlExists = "{{existsSql}}";
                      private const int KeyPropertyCount = {{keyCount}};
                  
                      public static async ValueTask<int> DeleteAsync(List<{{ctx.DtoTypeName}}> models, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                      {
                          if (models is null || models.Count == 0) return 0;
                          await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
                          
                          // SQL Server limits: 2100 parameters per command, 1000 rows per VALUES constructor.
                          const int RowsPerCommand = {{RowsPerCommand(keyCount)}};
                          var affected = 0;
                          
                          for (var start = 0; start < models.Count; start += RowsPerCommand)
                          {
                              var end = Math.Min(start + RowsPerCommand, models.Count);
                              // Pad to a power-of-two bucket by repeating the last key (harmless for DELETE),
                              // so the SQL text and its cached plan are shared across list sizes.
                              var slots = SqlParameterNames.BucketSize(end - start, RowsPerCommand);
                              
                              await using var cmd = new SqlCommand("", connection, transaction);
                              
                              if (commandTimeout.HasValue) cmd.CommandTimeout = commandTimeout.Value;
                              
                              cmd.EnableOptimizedParameterBinding = slots * KeyPropertyCount > 24;
                              
                              var sb = new StringBuilder(SqlDeleteBatchPrefix.Length + SqlDeleteBatchSuffix.Length + slots * (KeyPropertyCount * 7 + 2));
                              sb.Append(SqlDeleteBatchPrefix);
                              var paramIndex = 0;
                              
                              for (var row = 0; row < slots; row++)
                              {
                                  var model = models[Math.Min(start + row, end - 1)];
                                  if (row > 0) sb.Append(',');
                  {{ParameterBindingEmitter.BindKeysInlineLoop(ctx, "model", "paramIndex", "sb", "SqlParameterNames", 16)}}
                              }
                              
                              sb.Append(SqlDeleteBatchSuffix);
                              cmd.CommandText = sb.ToString();
                              
                              affected += await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                          }
                          
                          return affected;
                      }
                  
                      public static async ValueTask<bool> ExistsAsync({{ctx.DtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                      {
                          await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
                          await using var cmd = connection.CreateCommand(SqlExists, CommandType.Text, transaction, commandTimeout);
                  {{ParameterBindingEmitter.BindKeys(ctx, "model", 8)}}
                  
                          var scalar = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                          
                          return scalar is not null && scalar is not DBNull;
                      }
                  }
                  
                  """";
        }

        var truncateSql = SqlServerSqlTextBuilder.Truncate(ctx);

        // Update/Delete method bodies — vary based on whether a concurrency token is configured
        var concurrencyThrow = ctx.ConcurrencyToken is not null
            ? $$"""
                      var affected = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                      if (affected == 0) throw new global::AdoGen.SqlServer.SqlConcurrencyException("{{ctx.Profile.Schema}}.{{ctx.Profile.Table}}");
                      return affected;
              """
            : "        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);";
        var updateBody = concurrencyThrow;
        var deleteBody = concurrencyThrow;

        // TryUpdate/TryDelete: never throw. With a concurrency token, 0 affected rows triggers a key lookup on the
        // same command to tell a missing row (NotFound) from a stale token (Conflict).
        var tryWriteBody = ctx.ConcurrencyToken is not null
            ? $$"""
                      if (await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false) > 0) return SqlWriteResult.Applied;

                      cmd.CommandText = SqlExists;
                      cmd.Parameters.Clear();
              {{ParameterBindingEmitter.BindKeys(ctx, "model", 8)}}
                      var scalar = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                      return scalar is null or DBNull ? SqlWriteResult.NotFound : SqlWriteResult.Conflict;
              """
            : "        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false) > 0 ? SqlWriteResult.Applied : SqlWriteResult.NotFound;";

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
            {{ctx.Accessibility}} partial {{ctx.TypeKeyword}} {{dto.Name}} : ISqlDomainModel<{{ctx.DtoTypeName}}>
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
                    await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
                    
                    await using var cmd = connection.CreateCommand(SqlCreateTable, CommandType.Text, transaction, commandTimeout);
                    
                    await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                public static async ValueTask<int> InsertAsync({{ctx.DtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
                    
                    await using var cmd = connection.CreateCommand(SqlInsert, CommandType.Text, transaction, commandTimeout);
                    
            {{ParameterBindingEmitter.BindAll(ctx, "model", 8)}}
                    return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                public static async ValueTask<int> InsertAsync(List<{{ctx.DtoTypeName}}> models, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (models is null || models.Count == 0) return 0;
                    await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);

                    // SQL Server limits: 2100 parameters per command, 1000 rows per VALUES constructor.
                    // Larger lists are split into several commands; use a transaction for atomicity.
                    const int RowsPerCommand = {{RowsPerCommand(nonIdentityPropCount)}};
                    var affected = 0;

                    for (var start = 0; start < models.Count; start += RowsPerCommand)
                    {
                        var end = Math.Min(start + RowsPerCommand, models.Count);

                        await using var cmd = new SqlCommand("", connection, transaction);
                        
                        if (commandTimeout.HasValue) cmd.CommandTimeout = commandTimeout.Value;
                        
                        cmd.EnableOptimizedParameterBinding = ((end - start) * {{nonIdentityPropCount}}) > 24;

                        var sb = new StringBuilder(SqlInsertBatchTemplate.Length + (end - start) * {{ParameterBindingEmitter.BatchInsertPerRowEstimate(ctx)}});
                        sb.Append(SqlInsertBatchTemplate);

                        var paramIndex = 0;

                        for (var modelIndex = start; modelIndex < end; modelIndex++)
                        {
                            if (modelIndex > start) sb.Append(',');
            {{ParameterBindingEmitter.BindWritablesInlineLoop(ctx, "models[modelIndex]", "paramIndex", "sb", "SqlParameterNames", 16)}}
                        }

                        cmd.CommandText = sb.ToString();
                        
                        affected += await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                    }

                    return affected;
                }

                public static async ValueTask<int> UpdateAsync({{ctx.DtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
                    
                    await using var cmd = connection.CreateCommand(SqlUpdate, CommandType.Text, transaction, commandTimeout);
                    
            {{ParameterBindingEmitter.BindForUpdate(ctx, "model", 8)}}
            {{updateBody}}
                }

                public static async ValueTask<{{ctx.DtoTypeName}}> InsertAndReturnAsync({{ctx.DtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
                    
                    await using var cmd = connection.CreateCommand(SqlInsertAndReturn, CommandType.Text, transaction, commandTimeout);
                    
            {{ParameterBindingEmitter.BindAll(ctx, "model", 8)}}
                    await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult | CommandBehavior.SingleRow, ct).ConfigureAwait(false);
                    
                    if (await reader.ReadAsync(ct).ConfigureAwait(false)) return {{dto.Name}}.Map(reader);
                    
                    throw new InvalidOperationException("InsertAndReturnAsync produced no row.");
                }

                public static async ValueTask<int> DeleteAsync({{ctx.DtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
                    
                    await using var cmd = connection.CreateCommand(SqlDelete, CommandType.Text, transaction, commandTimeout);
                    
            {{ParameterBindingEmitter.BindForDelete(ctx, "model", 8)}}
            {{deleteBody}}
                }

                public static async ValueTask<SqlWriteResult> TryUpdateAsync({{ctx.DtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
                    
                    await using var cmd = connection.CreateCommand(SqlUpdate, CommandType.Text, transaction, commandTimeout);
                    
            {{ParameterBindingEmitter.BindForUpdate(ctx, "model", 8)}}
            {{tryWriteBody}}
                }

                public static async ValueTask<SqlWriteResult> TryDeleteAsync({{ctx.DtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
                    
                    await using var cmd = connection.CreateCommand(SqlDelete, CommandType.Text, transaction, commandTimeout);
                    
            {{ParameterBindingEmitter.BindForDelete(ctx, "model", 8)}}
            {{tryWriteBody}}
                }

                public static async ValueTask<int> UpsertAsync({{ctx.DtoTypeName}} model, SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
                    
                    await using var cmd = connection.CreateCommand(SqlUpsert, CommandType.Text, transaction, commandTimeout);
                    
            {{ParameterBindingEmitter.BindForUpsert(ctx, "model", 8)}}
                    return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                public static async ValueTask<int> TruncateAsync(SqlConnection connection, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);
                    
                    await using var cmd = connection.CreateCommand(SqlTruncate, CommandType.Text, transaction, commandTimeout);
                    
                    return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                public static void AddInsertBatchCommand(SqlBatch batch, {{ctx.DtoTypeName}} model)
                {
                    var batchCmd = new SqlBatchCommand(SqlInsert);
            {{ParameterBindingEmitter.BindAll(ctx, "model", 8, "batchCmd")}}
                    batch.BatchCommands.Add(batchCmd);
                }

                public static void AddUpdateBatchCommand(SqlBatch batch, {{ctx.DtoTypeName}} model)
                {
                    var batchCmd = new SqlBatchCommand(SqlUpdate);
            {{ParameterBindingEmitter.BindForUpdate(ctx, "model", 8, "batchCmd")}}
                    batch.BatchCommands.Add(batchCmd);
                }

                public static void AddDeleteBatchCommand(SqlBatch batch, {{ctx.DtoTypeName}} model)
                {
                    var batchCmd = new SqlBatchCommand(SqlDelete);
            {{ParameterBindingEmitter.BindForDelete(ctx, "model", 8, "batchCmd")}}
                    batch.BatchCommands.Add(batchCmd);
                }

                public static void AddUpsertBatchCommand(SqlBatch batch, {{ctx.DtoTypeName}} model)
                {
                    var batchCmd = new SqlBatchCommand(SqlUpsert);
            {{ParameterBindingEmitter.BindForUpsert(ctx, "model", 8, "batchCmd")}}
                    batch.BatchCommands.Add(batchCmd);
                }

                public static void AddInsertAndReturnBatchCommand(SqlBatch batch, {{ctx.DtoTypeName}} model)
                {
                    var batchCmd = new SqlBatchCommand(SqlInsertAndReturn);
            {{ParameterBindingEmitter.BindAll(ctx, "model", 8, "batchCmd")}}
                    batch.BatchCommands.Add(batchCmd);
                }
            }
            """";
        
        var (patchNested, patchTopLevel) = BuildPatchSrc(ctx, dto);
        var body = patchNested.Length == 0 ? src : src + "\n" + patchNested;
        var wrapped = TypeDeclarations.WrapInContainers(dto, body);

        spc.AddSource(TypeDeclarations.HintName(dto, "Domain.Sql.g.cs"), patchTopLevel.Length == 0 ? wrapped : wrapped + "\n\n" + patchTopLevel);
    }

    private static (string Nested, string TopLevel) BuildPatchSrc(EmitContext ctx, INamedTypeSymbol dto)
    {
        var patchColumns = ctx.WritableNonKeyNonIdentities;
        // Skip if no patchable columns or too many (>64 exceeds bitmask capacity)
        if (patchColumns.Length is 0 or > 64) return (string.Empty, string.Empty);

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

        var concurrencyField = PatchConcurrency.BuildMembers(ctx, patchClassName);

        var setClauseItems = new StringBuilder();
        // Upper bound of the final SQL length, so the patch StringBuilder never regrows.
        var patchSqlCapacity = $"UPDATE {ctx.SchemaTableQuoted} SET ".Length + $" WHERE {ctx.WhereByKey}".Length + 2;
        for (var i = 0; i < patchColumns.Length; i++)
        {
            var col = patchColumns[i];
            var assignment = $"[{col.ParameterName}] = @{col.ParameterName}";
            patchSqlCapacity += assignment.Length + 2;
            var append = i > 0 ? $"sb.Append(separator).Append(\"{assignment}\");" : $"sb.Append(\"{assignment}\");";
            setClauseItems.AppendLine(
                $"        if ((patch.Mask & (1UL << {i})) != 0) {{ {append} separator = \", \"; cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}(({col.PropertyType.TrimEnd('?')})patch.{col.Name}Value!)); }}");
        }

        if (ctx.ConcurrencyToken is { } patchToken)
            patchSqlCapacity += 2 * (patchToken.ColumnNameQuoted.Length + patchToken.ParameterName.Length) + 24;

        var concurrencySet = PatchConcurrency.BuildSet(ctx, "@");
        var concurrencyWhere = PatchConcurrency.BuildWhere(ctx);
        var concurrencyThrow = PatchConcurrency.BuildExecute(ctx, "global::AdoGen.SqlServer.SqlConcurrencyException");

        var patchClassBody = ctx.ShouldGeneratePatchClass
            ? $$""""

                /// <summary>
                /// Patch command object for <see cref="{{dto.Name}}"/>.
                /// Set only the properties you want to update; unset properties are not written to the database.
                /// </summary>
                {{ctx.Accessibility}} sealed partial class {{patchClassName}}
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

        var nested = $$""""
            {{patchClassBody}}
            {{ctx.Accessibility}} partial {{ctx.TypeKeyword}} {{dto.Name}}
            {
            #pragma warning disable CS0219 // generated code, will trigger warn when single property on class
            
                public static async ValueTask<int> PatchAsync(SqlConnection connection, {{patchClassName}} patch, CancellationToken ct, SqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (patch.Mask == 0UL) return 0;
                    await using var connectionScope = await SqlConnectionScope.OpenAsync(connection, ct).ConfigureAwait(false);

                    await using var cmd = new SqlCommand("", connection, transaction);
                    if (commandTimeout.HasValue) cmd.CommandTimeout = commandTimeout.Value;

                    var sb = new StringBuilder({{patchSqlCapacity}});
                    sb.Append("UPDATE {{ctx.SchemaTableQuoted}} SET ");
                    var separator = "";
                    
            {{setClauseItems.ToString().TrimEnd()}}
            {{concurrencySet}}
                    sb.Append(" WHERE {{ctx.WhereByKey}}");
            {{concurrencyWhere}}
                    cmd.CommandText = sb.ToString();
            {{keyBindBuilder.ToString().TrimEnd()}}
                    
            {{concurrencyThrow}}
                }
            
            #pragma warning restore CS0219
            }
            """";

        var qualifiedDto = TypeDeclarations.ContainerPrefix(dto) + dto.Name;
        var qualifiedPatch = TypeDeclarations.ContainerPrefix(dto) + patchClassName;
        var extensionsClassName = TypeDeclarations.ContainerPrefix(dto, "") + dto.Name + "SqlPatchExtensions";
        var extensionsAccessibility = TypeDeclarations.EffectiveAccessibility(dto);

        var topLevel = $$""""
            /// <summary>Extension methods for patching <see cref="{{qualifiedDto}}"/> via <see cref="SqlConnection"/>.</summary>
            {{extensionsAccessibility}} static class {{extensionsClassName}}
            {
                /// <summary>
                /// Executes a partial UPDATE for <see cref="{{qualifiedDto}}"/>, touching only the columns
                /// explicitly set on <paramref name="patch"/>.
                /// Returns 0 immediately (without sending SQL) when no columns were set.
                /// </summary>
                public static global::System.Threading.Tasks.ValueTask<int> PatchAsync(
                    this SqlConnection connection,
                    {{qualifiedPatch}} patch,
                    global::System.Threading.CancellationToken ct,
                    SqlTransaction? transaction = null,
                    int? commandTimeout = null)
                    => {{qualifiedDto}}.PatchAsync(connection, patch, ct, transaction, commandTimeout);
            }
            """";

        return (nested, topLevel);
    }

    /// <summary>Max rows per command that keeps under SQL Server's 2100-parameter and 1000-row VALUES limits.</summary>
    private static int RowsPerCommand(int parametersPerRow) =>
        Math.Min(1000, 2000 / Math.Max(1, parametersPerRow));

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
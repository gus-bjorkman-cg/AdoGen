using System.Text;
using AdoGen.Generator.Models;
using Microsoft.CodeAnalysis;

namespace AdoGen.Generator.Emitters.PostgreSql;

internal sealed class DomainOpsEmitterNpgSql : IEmitter
{
    private DomainOpsEmitterNpgSql() { }
    public static DomainOpsEmitterNpgSql Instance { get; } = new();
    
    public bool IsMatch(SqlModelKind kind, SqlProviderKind provider) =>
        provider is SqlProviderKind.PostgreSql && kind >= SqlModelKind.Domain;

    public void Handle(SourceProductionContext spc, ValidatedDiscoveryDto validatedDto, EmitContext ctx)
    {
        var (discoveryDto, profileInfo, _) = validatedDto;
        var dto = discoveryDto.Dto;
        var typeKeyword = dto.IsRecord ? "record" : "class";

        // SQL strings — produced by PostgreSqlSqlTextBuilder
        var createTableSql = PostgreSqlSqlTextBuilder.CreateTable(ctx);
        var insertSql = PostgreSqlSqlTextBuilder.Insert(ctx);
        var insertBatchSql = PostgreSqlSqlTextBuilder.InsertBatchPrefix(ctx);
        var insertAndReturnSql = PostgreSqlSqlTextBuilder.InsertAndReturn(ctx);
        var updateSql = PostgreSqlSqlTextBuilder.Update(ctx);
        var deleteSql = PostgreSqlSqlTextBuilder.Delete(ctx);
        var upsertSql = PostgreSqlSqlTextBuilder.Upsert(ctx);
        var truncateSql = PostgreSqlSqlTextBuilder.Truncate(ctx);
        var existsSql = PostgreSqlSqlTextBuilder.Exists(ctx);

        // Update/Delete/Upsert method bodies — vary based on whether a concurrency token is configured
        var updateBody = ctx.ConcurrencyToken is not null
            ? $$"""
                      var affected = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                          if (affected == 0) throw new global::AdoGen.PostgreSql.AdoGenConcurrencyException("{{ctx.Profile.Schema}}.{{ctx.Profile.Table}}");
                          return affected;
              """
            : "        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);";

        var deleteBody = ctx.ConcurrencyToken is not null
            ? $$"""
                      var affected = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                          if (affected == 0) throw new global::AdoGen.PostgreSql.AdoGenConcurrencyException("{{ctx.Profile.Schema}}.{{ctx.Profile.Table}}");
                          return affected;
              """
            : "        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);";


        var idBasedSrc = "";
        
        if (profileInfo.Keys.Length == 1)
        {
            var keyName = profileInfo.Keys[0];
            var keyCfg = profileInfo.ParamsByProperty[keyName];
            var keyType = keyCfg.PropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var keyDbType = keyCfg.DbType!.Value.EnumMember;
            var deleteBatchSql = PostgreSqlSqlTextBuilder.DeleteBatchAny(ctx, keyName);

            idBasedSrc =
                $$""""

                   {{ctx.Accessibility}} sealed partial {{typeKeyword}} {{dto.Name}} : INpgsqlSingleIdModel<{{ctx.DtoTypeName}}, {{keyType}}>
                   {
                       private const string Pg_SqlDeleteBatchAny = """{{deleteBatchSql}}""";
                       private const string Pg_SqlExists = """{{existsSql}}""";
                   
                       public static async ValueTask<int> DeleteAsync(NpgsqlConnection connection, List<{{keyType}}> ids, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
                       {
                           if (ids is null || ids.Count == 0) return 0;
                           if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                   
                           await using var cmd = connection.CreateCommand(Pg_SqlDeleteBatchAny, CommandType.Text, transaction, commandTimeout);
                           
                           cmd.Parameters.Add(new NpgsqlParameter
                           {
                               ParameterName = "ids",
                               NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.{{keyDbType}},
                               Value = ids.ToArray(),
                           });
                           
                           return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                       }
                   
                       public static async ValueTask<bool> ExistsAsync(NpgsqlConnection connection, {{keyType}} id, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
                       {
                           if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                           
                           await using var cmd = connection.CreateCommand(Pg_SqlExists, CommandType.Text, transaction, commandTimeout);
                           
                           cmd.Parameters.Add({{dto.Name}}Npgsql.CreateParameter{{keyName}}(id));
                           var scalar = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                           
                           return scalar is not null && scalar is not DBNull;
                       }
                   }

                   """";
        }
        else
        {
            var deleteBatchUnnestSql = PostgreSqlSqlTextBuilder.DeleteBatchUnnest(ctx);
            var compositeKeyArrayBindings = ParameterBindingEmitter.BindKeyArraysNpgsql(ctx, "models", 12);

            idBasedSrc =
                $$""""

                  {{ctx.Accessibility}} sealed partial {{typeKeyword}} {{dto.Name}} : INpgsqlCompositeKeyModel<{{ctx.DtoTypeName}}>, INpgsqlCompositeKeyExistsModel<{{ctx.DtoTypeName}}>
                  {
                      private const string Pg_SqlDeleteBatchUnnest = """{{deleteBatchUnnestSql}}""";
                      private const string Pg_SqlExists = """{{existsSql}}""";

                      public static async ValueTask<int> DeleteAsync(List<{{ctx.DtoTypeName}}> models, NpgsqlConnection connection, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
                      {
                          if (models is null || models.Count == 0) return 0;
                          if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);

                          await using var cmd = connection.CreateCommand(Pg_SqlDeleteBatchUnnest, CommandType.Text, transaction, commandTimeout);
                          
                  {{compositeKeyArrayBindings}}
                  
                          return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                      }

                      public static async ValueTask<bool> ExistsAsync({{ctx.DtoTypeName}} model, NpgsqlConnection connection, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
                      {
                          if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                          
                          await using var cmd = connection.CreateCommand(Pg_SqlExists, CommandType.Text, transaction, commandTimeout);
                          
                  {{ParameterBindingEmitter.BindKeys(ctx, "model", 8)}}
                  
                          var scalar = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                          
                          return scalar is not null && scalar is not DBNull;
                      }
                  }

                  """";
        }

        var src =
            $$""""
              // <auto-generated />
              #nullable enable
              using System;
              using System.Data;
              using System.Text;
              using System.Collections.Generic;
              using System.Threading;
              using System.Threading.Tasks;
              using System.Linq;
              using Npgsql;
              using NpgsqlTypes;
              using AdoGen.PostgreSql;

              namespace {{ctx.Namespace}};
              {{idBasedSrc}}
              {{ctx.Accessibility}} sealed partial {{typeKeyword}} {{dto.Name}} : INpgsqlDomainModel<{{ctx.DtoTypeName}}>
              {
                  private const string Pg_SqlCreateTable = 
                  """
              {{createTableSql}}
                  """;
                  
                  private const string Pg_SqlInsert = """{{insertSql}}""";
                  private const string Pg_SqlInsertBatchTemplate = """{{insertBatchSql}}""";
                  private const string Pg_SqlInsertAndReturn = """{{insertAndReturnSql}}""";
                  private const string Pg_SqlUpdate = """{{updateSql}}""";
                  private const string Pg_SqlDelete = """{{deleteSql}}""";
                  private const string Pg_SqlTruncate = """{{truncateSql}}""";
                  private const string Pg_SqlUpsert = """{{upsertSql}}""";

                  public static async ValueTask CreateTableAsync(NpgsqlConnection connection, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
                  {
                      if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                      
                      await using var cmd = connection.CreateCommand(Pg_SqlCreateTable, CommandType.Text, transaction, commandTimeout);
                      
                      await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                  }

                  public static async ValueTask<int> InsertAsync({{ctx.DtoTypeName}} model, NpgsqlConnection connection, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
                  {
                      if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                      
                      await using var cmd = connection.CreateCommand(Pg_SqlInsert, CommandType.Text, transaction, commandTimeout);
                      
              {{ParameterBindingEmitter.BindAll(ctx, "model", 8)}}
                      return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                  }

                  public static async ValueTask<int> InsertAsync(List<{{ctx.DtoTypeName}}> models, NpgsqlConnection connection, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
                  {
                      if (models is null || models.Count == 0) return 0;
                      
                      if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);

                      await using var cmd = new NpgsqlCommand("", connection, transaction);
                      
                      if (commandTimeout.HasValue) cmd.CommandTimeout = commandTimeout.Value;

                      var sb = new StringBuilder(Pg_SqlInsertBatchTemplate.Length + models.Count * {{ParameterBindingEmitter.BatchInsertPerRowEstimate(ctx)}});
                      sb.Append(Pg_SqlInsertBatchTemplate);
                      var paramIndex = 0;

                      for (var modelIndex = 0; modelIndex < models.Count; modelIndex++)
                      {
                          if (modelIndex > 0) sb.Append(',');
              {{ParameterBindingEmitter.BindWritablesInlineLoop(ctx, "models[modelIndex]", "paramIndex", "sb", 12)}}
                      }

                      cmd.CommandText = sb.ToString();
                      
                      return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                  }

                  public static async ValueTask<{{ctx.DtoTypeName}}> InsertAndReturnAsync({{ctx.DtoTypeName}} model, NpgsqlConnection connection, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
                  {
                      if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                      
                      await using var cmd = connection.CreateCommand(Pg_SqlInsertAndReturn, CommandType.Text, transaction, commandTimeout);
                      
              {{ParameterBindingEmitter.BindAll(ctx, "model", 8)}}
                      await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, ct).ConfigureAwait(false);
                      
                      if (await reader.ReadAsync(ct).ConfigureAwait(false)) return {{dto.Name}}.Map(reader);
                      
                      throw new InvalidOperationException("InsertAndReturnAsync produced no row.");
                  }

                  public static async ValueTask<int> UpdateAsync({{ctx.DtoTypeName}} model, NpgsqlConnection connection, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
                  {
                      if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                      
                      await using var cmd = connection.CreateCommand(Pg_SqlUpdate, CommandType.Text, transaction, commandTimeout);
                      
              {{ParameterBindingEmitter.BindForUpdate(ctx, "model", 8)}}        
              {{updateBody}}
                  }

                  public static async ValueTask<int> DeleteAsync({{ctx.DtoTypeName}} model, NpgsqlConnection connection, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
                  {
                      if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                      
                      await using var cmd = connection.CreateCommand(Pg_SqlDelete, CommandType.Text, transaction, commandTimeout);
                      
              {{ParameterBindingEmitter.BindForDelete(ctx, "model", 8)}}        
              {{deleteBody}}
                  }

                  public static async ValueTask<int> UpsertAsync({{ctx.DtoTypeName}} model, NpgsqlConnection connection, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
                  {
                      if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                      
                      await using var cmd = connection.CreateCommand(Pg_SqlUpsert, CommandType.Text, transaction, commandTimeout);
                      
              {{ParameterBindingEmitter.BindAll(ctx, "model", 8)}}
                      return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                  }
    
                  public static async ValueTask<int> TruncateAsync(NpgsqlConnection connection, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
                  {
                      if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                      
                      await using var cmd = connection.CreateCommand(Pg_SqlTruncate, CommandType.Text, transaction, commandTimeout);
                      
                      return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                  }
              }
              """";

        var patchSrc = BuildPatchSrc(ctx, dto, typeKeyword);

        spc.AddSource($"{dto.Name}.Domain.Npgsql.g.cs", patchSrc.Length == 0 ? src : src + "\n" + patchSrc);
    }

    private static string BuildPatchSrc(EmitContext ctx, INamedTypeSymbol dto, string typeKeyword)
    {
        var patchColumns = ctx.WritableNonKeyNonIdentities;
        // Skip if no patchable columns or too many (>64 exceeds bitmask capacity)
        if (patchColumns.Length is 0 or > 64) return string.Empty;

        var patchClassName = $"{dto.Name}Patch";
        var schemaTableEscaped = ctx.SchemaTableQuoted.Replace("\"", "\\\"");

        // Key bind for WHERE clause
        var keyBindBuilder = new StringBuilder();
        for (var k = 0; k < ctx.Keys.Length; k++)
        {
            var key = ctx.Keys[k];
            keyBindBuilder.AppendLine($"        cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{key.Name}(patch.{key.Name}));");
        }

        var setClauseItems = new StringBuilder();
        for (var i = 0; i < patchColumns.Length; i++)
        {
            var col = patchColumns[i];
            var colSql = $"\\\"{col.ParameterName}\\\" = @{col.ParameterName}";
            setClauseItems.AppendLine(
                $"        if ((patch.Mask & (1UL << {i})) != 0) {{ sb.Append({(i > 0 ? "separator + " : "")}\"{colSql}\"); separator = \", \"; cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{col.Name}(({col.PropertyType.TrimEnd('?')})patch.{col.Name}Value!)); }}");
        }

        // WHERE clause from keys
        var whereByKeyPg = new StringBuilder();
        for (var k = 0; k < ctx.Keys.Length; k++)
        {
            if (k > 0) whereByKeyPg.Append(" AND ");
            whereByKeyPg.Append($"\\\"{ctx.Keys[k].ParameterName}\\\" = @{ctx.Keys[k].ParameterName}");
        }

        var concurrencyWhere = ctx.ConcurrencyToken is { } token
            ? $"            cmd.CommandText += \" AND \\\"{token.ParameterName}\\\" = @{token.ParameterName}\";\n" +
              $"            cmd.Parameters.Add({ctx.FactoryClassName}.CreateParameter{token.Name}(patch.{token.Name}Value));\n"
            : "";

        var concurrencyThrow = ctx.ConcurrencyToken is not null
            ? $$"""
                        var affected = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                        if (affected == 0) throw new global::AdoGen.PostgreSql.AdoGenConcurrencyException("{{ctx.Profile.Schema}}.{{ctx.Profile.Table}}");
                        return affected;
              """
            : "        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);";

        // Build class body (only when this provider owns the shared {Dto}Patch class)
        var keyPropsBuilder = new StringBuilder();
        var ctorParamsBuilder = new StringBuilder();
        var ctorAssignBuilder = new StringBuilder();
        for (var k = 0; k < ctx.Keys.Length; k++)
        {
            var key = ctx.Keys[k];
            if (k > 0) ctorParamsBuilder.Append(", ");
            keyPropsBuilder.AppendLine($"    public {key.PropertyType} {key.Name} {{ get; }}");
            ctorParamsBuilder.Append($"{key.PropertyType} {LowerFirst(key.Name)}");
            ctorAssignBuilder.Append($"{key.Name} = {LowerFirst(key.Name)}; ");
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

        var patchClassBody = ctx.ShouldGeneratePatchClass
            ? $$""""

                /// <summary>
                /// Patch command object for <see cref="{{dto.Name}}"/>.
                /// Set only the properties you want to update; unset properties are not written to the database.
                /// </summary>
                public sealed partial class {{patchClassName}}
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
            {{ctx.Accessibility}} sealed partial {{typeKeyword}} {{dto.Name}}
            {
                public static async ValueTask<int> PatchAsync(NpgsqlConnection connection, {{patchClassName}} patch, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
                {
                    if (patch.Mask == 0UL) return 0;
                    if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);

                    await using var cmd = new NpgsqlCommand("", connection, transaction);
                    if (commandTimeout.HasValue) cmd.CommandTimeout = commandTimeout.Value;

                    var sb = new StringBuilder("UPDATE {{schemaTableEscaped}} SET ");
                    var separator = "";
                    
            {{setClauseItems.ToString().TrimEnd()}}
            
                    sb.Append(" WHERE {{whereByKeyPg}}");
            {{concurrencyWhere.TrimEnd()}}
                    cmd.CommandText = sb.ToString();
            {{keyBindBuilder.ToString().TrimEnd()}}
                    
            {{concurrencyThrow}}
                }
            }

            /// <summary>Extension methods for patching <see cref="{{dto.Name}}"/> via <see cref="NpgsqlConnection"/>.</summary>
            public static class {{dto.Name}}NpgsqlPatchExtensions
            {
                /// <summary>
                /// Executes a partial UPDATE for <see cref="{{dto.Name}}"/>, touching only the columns
                /// explicitly set on <paramref name="patch"/>.
                /// Returns 0 immediately (without sending SQL) when no columns were set.
                /// </summary>
                public static global::System.Threading.Tasks.ValueTask<int> PatchAsync(
                    this NpgsqlConnection connection,
                    {{patchClassName}} patch,
                    global::System.Threading.CancellationToken ct,
                    NpgsqlTransaction? transaction = null,
                    int? commandTimeout = null)
                    => {{dto.Name}}.PatchAsync(connection, patch, ct, transaction, commandTimeout);
            }
            """";
    }

    private static string LowerFirst(string s)
    {
        if (s.Length == 0) return s;
        var result = char.ToLowerInvariant(s[0]) + s.Substring(1);
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
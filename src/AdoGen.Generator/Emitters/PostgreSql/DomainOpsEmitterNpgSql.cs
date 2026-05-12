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
            var keyType = profileInfo.ParamsByProperty[keyName].PropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var deleteBatchSql = PostgreSqlSqlTextBuilder.DeleteBatchTemplate(ctx, keyName);

            idBasedSrc =
                $$""""

                   {{ctx.Accessibility}} sealed partial {{typeKeyword}} {{dto.Name}} : INpgsqlSingleIdModel<{{ctx.DtoTypeName}}, {{keyType}}>
                   {
                       private const string Pg_SqlDeleteBatchTemplate = """{{deleteBatchSql}}""";
                       private const string Pg_SqlExists = """{{existsSql}}""";
                   
                       public static async ValueTask<int> DeleteAsync(NpgsqlConnection connection, List<{{keyType}}> ids, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
                       {
                           if (ids is null || ids.Count == 0) return 0;
                           if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);
                   
                           await using var cmd = new NpgsqlCommand("", connection, transaction);
                           if (commandTimeout.HasValue) cmd.CommandTimeout = commandTimeout.Value;
                   
                           var sb = new StringBuilder(Pg_SqlDeleteBatchTemplate);
                           for (var i = 0; i < ids.Count; i++)
                           {
                               if (i > 0) sb.Append(',');
                               sb.Append($"@p{i}");
                               cmd.Parameters.Add({{dto.Name}}Npgsql.CreateParameter{{keyName}}(ids[i], $"@p{i}"));
                           }
                           sb.Append(')');
                           cmd.CommandText = sb.ToString();
                   
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
            var deleteBatchPrefix = PostgreSqlSqlTextBuilder.DeleteBatchJoinValuesPrefix(ctx);
            var deleteBatchSuffix = PostgreSqlSqlTextBuilder.DeleteBatchJoinValuesSuffix(ctx);

            idBasedSrc =
                $$""""

                  {{ctx.Accessibility}} sealed partial {{typeKeyword}} {{dto.Name}} : INpgsqlCompositeKeyModel<{{ctx.DtoTypeName}}>, INpgsqlCompositeKeyExistsModel<{{ctx.DtoTypeName}}>
                  {
                      private const string Pg_SqlDeleteBatchPrefix = "{{deleteBatchPrefix.Replace("\"", "\\\"")}}";
                      private const string Pg_SqlDeleteBatchSuffix = "{{deleteBatchSuffix.Replace("\"", "\\\"")}}";
                      private const string Pg_SqlExists = """{{existsSql}}""";

                      public static async ValueTask<int> DeleteAsync(List<{{ctx.DtoTypeName}}> models, NpgsqlConnection connection, CancellationToken ct, NpgsqlTransaction? transaction = null, int? commandTimeout = null)
                      {
                          if (models is null || models.Count == 0) return 0;
                          if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct).ConfigureAwait(false);

                          await using var cmd = new NpgsqlCommand("", connection, transaction);
                          if (commandTimeout.HasValue) cmd.CommandTimeout = commandTimeout.Value;

                          var sb = new StringBuilder(Pg_SqlDeleteBatchPrefix);
                          var paramIndex = 0;

                          for (var i = 0; i < models.Count; i++)
                          {
                              if (i > 0) sb.Append(',');
                  {{ParameterBindingEmitter.BindKeysInlineLoop(ctx, "models[i]", "paramIndex", "sb", 12)}}
                          }
                          sb.Append(Pg_SqlDeleteBatchSuffix);
                          cmd.CommandText = sb.ToString();

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
              using Npgsql;
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

        spc.AddSource($"{dto.Name}.Domain.Npgsql.g.cs", src);
    }
}
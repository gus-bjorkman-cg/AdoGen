using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using AdoGen.Generator.Models;

namespace AdoGen.Generator.Emitters.SqlServer;

/// <summary>
/// Pure SQL string production for SQL Server. All methods are stateless and testable
/// with hand-built EmitContext fixtures — no Roslyn compilation required.
/// </summary>
internal static class SqlServerSqlTextBuilder
{
    public static string CreateTable(EmitContext ctx)
    {
        var sbColDefs = new StringBuilder();
        for (var i = 0; i < ctx.Columns.Length; i++)
        {
            var col = ctx.Columns[i];
            var nullability = col.IsNullable ? "NULL" : "NOT NULL";
            var identity = col.IsIdentity ? " IDENTITY(1,1)" : "";
            var defaultClause = col.DefaultSqlExpression is not null ? $" {col.DefaultSqlExpression}" : "";
            var comma = i == ctx.Columns.Length - 1 ? "" : ",";
            sbColDefs.AppendLine($"            {col.ColumnNameQuoted} {col.SqlType}{identity}{defaultClause} {nullability}{comma}");
        }

        if (ctx.Keys.Length > 0)
        {
            var pkCols = BuildJoined(ctx.Keys, col => col.ColumnNameQuoted);
            sbColDefs.AppendLine($"        ,CONSTRAINT [PK_{ctx.Profile.Table}] PRIMARY KEY ({pkCols})");
        }

        var colDefs = sbColDefs.ToString().TrimEnd();
        return $"""
            CREATE TABLE {ctx.SchemaTableQuoted}(
            {colDefs});
            """;
    }

    public static string Insert(EmitContext ctx)
    {
        var insertCols = BuildJoined(ctx.Writables, col => col.ColumnNameQuoted);
        var insertParams = BuildJoined(ctx.Writables, col => "@" + col.ParameterName);
        return $"INSERT INTO {ctx.SchemaTableQuoted} ({insertCols}) VALUES ({insertParams});";
    }

    /// <summary>
    /// Returns INSERT … OUTPUT INSERTED.* VALUES (…) — populates all columns including server-generated ones.
    /// Note: fails in the presence of certain triggers. Document this in InsertAndReturnAsync XML docs.
    /// </summary>
    public static string InsertAndReturn(EmitContext ctx)
    {
        var insertCols = BuildJoined(ctx.Writables, col => col.ColumnNameQuoted);
        var insertParams = BuildJoined(ctx.Writables, col => "@" + col.ParameterName);
        return $"INSERT INTO {ctx.SchemaTableQuoted} ({insertCols}) OUTPUT INSERTED.* VALUES ({insertParams});";
    }

    public static string InsertBatchPrefix(EmitContext ctx)
    {
        var insertCols = BuildJoined(ctx.Writables, col => col.ColumnNameQuoted);
        return $"INSERT INTO {ctx.SchemaTableQuoted} ({insertCols}) VALUES";
    }

    public static string Update(EmitContext ctx)
    {
        var updateSet = BuildJoined(ctx.WritableNonKeyNonIdentities, col => $"{col.ColumnNameQuoted} = @{col.ParameterName}");
        if (ctx.ConcurrencyToken is { } token)
        {
            var tokenBump = IsIntOrLong(token.PropertyType)
                ? $"{token.ColumnNameQuoted} = @{token.ParameterName} + 1"
                : $"{token.ColumnNameQuoted} = NEWID()";
            return $"UPDATE {ctx.SchemaTableQuoted} SET {updateSet}, {tokenBump} WHERE {ctx.WhereByKey} AND {token.ColumnNameQuoted} = @{token.ParameterName};";
        }
        return $"UPDATE {ctx.SchemaTableQuoted} SET {updateSet} WHERE {ctx.WhereByKey};";
    }

    public static string Delete(EmitContext ctx)
    {
        var tokenClause = ctx.ConcurrencyToken is { } token
            ? $" AND {token.ColumnNameQuoted} = @{token.ParameterName}"
            : "";
        return $"DELETE FROM {ctx.SchemaTableQuoted} WHERE {ctx.WhereByKey}{tokenClause};";
    }

    public static string Upsert(EmitContext ctx)
    {
        var updateSet = BuildJoined(ctx.WritableNonKeyNonIdentities, col => $"{col.ColumnNameQuoted} = @{col.ParameterName}");
        var insertCols = BuildJoined(ctx.Writables, col => col.ColumnNameQuoted);
        var insertParams = BuildJoined(ctx.Writables, col => "@" + col.ParameterName);

        return $"UPDATE {ctx.SchemaTableQuoted} SET {updateSet} WHERE {ctx.WhereByKey}; " +
               $"IF @@ROWCOUNT = 0 INSERT INTO {ctx.SchemaTableQuoted} ({insertCols}) VALUES ({insertParams});";
    }

    public static string Truncate(EmitContext ctx)
        => $"TRUNCATE TABLE {ctx.SchemaTableQuoted};";

    /// <summary>
    /// Returns a SELECT TOP(1) 1 FROM … WHERE pk1 = @pk1 [AND pk2 = @pk2] statement.
    /// TOP(1) ensures the engine stops scanning after the first matching row.
    /// </summary>
    public static string Exists(EmitContext ctx)
        => $"SELECT TOP(1) 1 FROM {ctx.SchemaTableQuoted} WHERE {ctx.WhereByKey};";

    /// <summary>
    /// Returns the static prefix of the DELETE…JOIN(VALUES…) statement.
    /// The caller appends "(…), (…)" rows and the ON clause dynamically.
    /// Example for single key:    "DELETE t FROM [dbo].[T] AS t JOIN (VALUES "
    /// Example for composite key: same prefix — column aliases and ON clause are appended by generated code.
    /// </summary>
    public static string DeleteBatchJoinValuesPrefix(EmitContext ctx)
        => $"DELETE t FROM {ctx.SchemaTableQuoted} AS t JOIN (VALUES ";
    
    /// <summary>
    /// Returns the closing " AS ids(...) ON ..." fragment that is appended once after all value rows.
    /// e.g. " AS ids([TenantId],[Id]) ON ids.[TenantId]=t.[TenantId] AND ids.[Id]=t.[Id]"
    /// </summary>
    public static string DeleteBatchJoinValuesSuffix(EmitContext ctx)
    {
        var aliasCols = BuildJoined(ctx.Keys, col => col.ColumnNameQuoted);
        var onClauses = BuildJoined(ctx.Keys, col => $"ids.{col.ColumnNameQuoted}=t.{col.ColumnNameQuoted}", " AND ");
        return $") AS ids({aliasCols}) ON {onClauses}";
    }

    public static string BulkCreateTempTable(EmitContext ctx, string tempTableName)
    {
        var sbColDefs = new StringBuilder();
        for (var i = 0; i < ctx.BulkColumns.Length; i++)
        {
            var col = ctx.BulkColumns[i];
            var nullability = col.IsNullable ? "NULL" : "NOT NULL";
            sbColDefs.AppendLine($"            {col.ColumnNameQuoted} {col.SqlType} {nullability},");
        }
        sbColDefs.Append("            [Operation] CHAR(1) NOT NULL");
        var colDefs = sbColDefs.ToString();

        return $"""
             CREATE TABLE {tempTableName}(
             {colDefs});
             """;
    }

    public static string BulkApply(EmitContext ctx, string tempTableName, BulkApplyOptions options)
    {
        var schemaTable = ctx.SchemaTableQuoted;
        var joinOn = ctx.JoinOn;
        var idxCols = BuildJoined(ctx.Keys, col => col.ColumnNameQuoted);
        var idxClause = $"        CREATE INDEX [IX_AdoGen_{ctx.Profile.Table}_Op_Key] ON {tempTableName} ([Operation], {idxCols});";
        var updateSet = string.Join(",\n        ", Enumerable.Select(ctx.WritableNonKeyNonIdentities,
            col => $"    T.{col.ColumnNameQuoted} = S.{col.ColumnNameQuoted}"));
        var insertCols = BuildJoined(ctx.Writables, col => col.ColumnNameQuoted);
        var insertSelect = BuildJoined(ctx.Writables, col => $"S.{col.ColumnNameQuoted}");

        var sb = new StringBuilder();

        sb.AppendLine("BEGIN TRY");
        sb.AppendLine("        DECLARE @inserted INT = 0, @updated INT = 0, @deleted INT = 0, @upserted INT = 0;");
        sb.AppendLine(idxClause);
        sb.AppendLine();

        if (options.HasUpdates && ctx.WritableNonKeyNonIdentities.Length > 0)
        {
            sb.AppendLine("        UPDATE T");
            sb.AppendLine("        SET");
            sb.AppendLine("        " + updateSet);
            sb.AppendLine($"        FROM {schemaTable} AS T");
            sb.AppendLine($"            JOIN {tempTableName} AS S ON {joinOn}");
            sb.AppendLine("        WHERE S.[Operation] = 'U';");
            sb.AppendLine("        SET @updated = @@ROWCOUNT;");
            sb.AppendLine();
        }

        if (options.HasInserts && ctx.Writables.Length > 0)
        {
            sb.AppendLine($"        INSERT INTO {schemaTable} ({insertCols})");
            sb.AppendLine($"        SELECT {insertSelect}");
            sb.AppendLine($"        FROM {tempTableName} AS S");
            sb.AppendLine("        WHERE S.[Operation] = 'I';");
            sb.AppendLine("        SET @inserted = @@ROWCOUNT;");
            sb.AppendLine();
        }

        sb.AppendLine("        DELETE T");
        sb.AppendLine($"        FROM {schemaTable} AS T");
        sb.AppendLine($"            JOIN {tempTableName} AS S ON {joinOn}");
        sb.AppendLine("        WHERE S.[Operation] = 'D';");
        sb.AppendLine("        SET @deleted = @@ROWCOUNT;");
        sb.AppendLine();

        if (options.HasUpserts && ctx.WritableNonKeyNonIdentities.Length > 0 && ctx.Writables.Length > 0)
        {
            sb.AppendLine("        UPDATE T");
            sb.AppendLine("        SET");
            sb.AppendLine("        " + updateSet);
            sb.AppendLine($"        FROM {schemaTable} AS T");
            sb.AppendLine($"            JOIN {tempTableName} AS S ON {joinOn}");
            sb.AppendLine("        WHERE S.[Operation] = 'M';");
            sb.AppendLine("        SET @upserted = @@ROWCOUNT;");
            sb.AppendLine();
            sb.AppendLine($"        INSERT INTO {schemaTable} ({insertCols})");
            sb.AppendLine($"        SELECT {insertSelect}");
            sb.AppendLine($"        FROM {tempTableName} AS S");
            sb.AppendLine($"        WHERE S.[Operation] = 'M' AND NOT EXISTS (SELECT 1 FROM {schemaTable} AS T WHERE {joinOn});");
            sb.AppendLine("        SET @upserted = @upserted + @@ROWCOUNT;");
            sb.AppendLine();
        }

        sb.AppendLine("        SELECT @inserted AS Inserted, @updated AS Updated, @deleted AS Deleted, @upserted AS Upserted;");
        sb.AppendLine();
        sb.AppendLine("        END TRY");
        sb.AppendLine("        BEGIN CATCH");
        sb.AppendLine($"    {DropGuard(tempTableName)}");
        sb.AppendLine("            THROW;");
        sb.AppendLine("        END CATCH;");
        sb.AppendLine(DropGuard(tempTableName));

        return sb.ToString().TrimEnd();
    }

    private static string DropGuard(string name)
        => $"        IF OBJECT_ID('tempdb..{name}') IS NOT NULL DROP TABLE {name};";

    private static bool IsIntOrLong(string propertyType)
        => propertyType is "int" or "long" or "global::System.Int32" or "global::System.Int64";
    
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

/// <summary>Options controlling which DML operations to include in the BulkApply SQL.</summary>
internal readonly record struct BulkApplyOptions(bool HasInserts, bool HasUpdates, bool HasUpserts = true);

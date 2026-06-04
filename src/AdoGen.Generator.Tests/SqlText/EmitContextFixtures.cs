using System.Collections.Immutable;
using AdoGen.Generator.Emitters;
using AdoGen.Generator.Emitters.PostgreSql;
using AdoGen.Generator.Emitters.SqlServer;
using AdoGen.Generator.Models;

namespace AdoGen.Generator.Tests.SqlText;

internal static class EmitContextFixtures
{
    // ── SQL Server ────────────────────────────────────────────────────────────

    /// <summary>User: single non-identity key (Guid Id with DEFAULT NEWID()), two plain string columns.</summary>
    public static EmitContext SqlServerUser() => Build(
        SqlProviderKind.SqlServer, SqlServerIdentifierQuoter.Instance, "dbo", "Users",
        [
            new ColumnInfo("Id", "[Id]", "Id", "UNIQUEIDENTIFIER", "global::System.Guid", false, false, true, false, false, "DEFAULT NEWID()", ColumnRole.Key),
            new ColumnInfo("Name", "[Name]", "Name", "VARCHAR(20)", "global::System.String", false, false, false, false, false, null, ColumnRole.Plain),
            new ColumnInfo("Email", "[Email]", "Email", "VARCHAR(50)", "global::System.String", false, false, false, false, false, null, ColumnRole.Plain)
        ]);

    /// <summary>AuditEvent: identity key (long EventId), schema="log", renamed column (Type).</summary>
    public static EmitContext SqlServerAuditEvent() => Build(
        SqlProviderKind.SqlServer, SqlServerIdentifierQuoter.Instance, "log", "Audits",
        [
            new ColumnInfo("EventId", "[EventId]", "EventId", "BIGINT", "global::System.Int64", false, true, true, false, false, null, ColumnRole.Identity),
            new ColumnInfo("CreatedAt", "[CreatedAt]", "CreatedAt", "DATETIMEOFFSET", "global::System.DateTimeOffset", false, false, false, false, false, null, ColumnRole.Plain),
            new ColumnInfo("EventType", "[Type]", "Type", "NVARCHAR(50)", "global::System.String", false, false, false, false, false, null, ColumnRole.Plain),
            new ColumnInfo("JsonPayload", "[JsonPayload]", "JsonPayload", "VARBINARY(8000)", "global::System.Byte[]", false, false, false, false, false, null, ColumnRole.Plain)
        ]);

    /// <summary>Order: single non-identity key (Guid Id), one plain string column — no identity column.</summary>
    public static EmitContext SqlServerOrder() => Build(
        SqlProviderKind.SqlServer, SqlServerIdentifierQuoter.Instance, "dbo", "Orders",
        [
            new ColumnInfo("Id", "[Id]", "Id", "UNIQUEIDENTIFIER", "global::System.Guid", false, false, true, false, false, null, ColumnRole.Key),
            new ColumnInfo("ProductName", "[ProductName]", "ProductName", "VARCHAR(100)", "global::System.String", false, false, false, false, false, null, ColumnRole.Plain)
        ]);

    /// <summary>Composite key: two non-identity key columns plus one plain value column.</summary>
    public static EmitContext SqlServerCompositeKey() => Build(
        SqlProviderKind.SqlServer, SqlServerIdentifierQuoter.Instance, "dbo", "OrderLines",
        [
            new ColumnInfo("OrderId", "[OrderId]", "OrderId", "UNIQUEIDENTIFIER", "global::System.Guid", false, false, true, false, false, null, ColumnRole.Key),
            new ColumnInfo("ProductId", "[ProductId]", "ProductId", "UNIQUEIDENTIFIER", "global::System.Guid", false, false, true, false, false, null, ColumnRole.Key),
            new ColumnInfo("Quantity", "[Quantity]", "Quantity", "INT", "global::System.Int32", false, false, false, false, false, null, ColumnRole.Plain)
        ]);

    // ── PostgreSQL ────────────────────────────────────────────────────────────

    /// <summary>User: single non-identity key (Guid Id), two plain string columns.</summary>
    public static EmitContext PostgreSqlUser() => Build(
        SqlProviderKind.PostgreSql, PostgreSqlIdentifierQuoter.Instance, "public", "Users",
        [
            new ColumnInfo("Id", "\"Id\"", "Id", "UUID", "global::System.Guid", false, false, true, false, false, "DEFAULT gen_random_uuid()", ColumnRole.Key),
            new ColumnInfo("Name", "\"Name\"", "Name", "VARCHAR(20)", "global::System.String", false, false, false, false, false, null, ColumnRole.Plain),
            new ColumnInfo("Email", "\"Email\"", "Email", "VARCHAR(50)", "global::System.String", false, false, false, false, false, null, ColumnRole.Plain)
        ]);

    /// <summary>Order: single non-identity key (Guid Id), one plain string column — no identity column.</summary>
    public static EmitContext PostgreSqlOrder() => Build(
        SqlProviderKind.PostgreSql, PostgreSqlIdentifierQuoter.Instance, "public", "Orders",
        [
            new ColumnInfo("Id", "\"Id\"", "Id", "uuid", "global::System.Guid", false, false, true, false, false, null, ColumnRole.Key),
            new ColumnInfo("ProductName", "\"ProductName\"", "ProductName", "varchar(100)", "global::System.String", false, false, false, false, false, null, ColumnRole.Plain)
        ]);

    /// <summary>Composite key: two non-identity key columns plus one plain value column.</summary>
    public static EmitContext PostgreSqlCompositeKey() => Build(
        SqlProviderKind.PostgreSql, PostgreSqlIdentifierQuoter.Instance, "public", "OrderLines",
        [
            new ColumnInfo("OrderId", "\"OrderId\"", "OrderId", "UUID", "global::System.Guid", false, false, true, false, false, null, ColumnRole.Key),
            new ColumnInfo("ProductId", "\"ProductId\"", "ProductId", "UUID", "global::System.Guid", false, false, true, false, false, null, ColumnRole.Key),
            new ColumnInfo("Quantity", "\"Quantity\"", "Quantity", "INT", "global::System.Int32", false, false, false, false, false, null, ColumnRole.Plain)
        ]);

    /// <summary>AuditEvent: identity key (bigint EventId), schema-qualified, plus three plain columns.</summary>
    public static EmitContext PostgreSqlAuditEvent() => Build(
        SqlProviderKind.PostgreSql, PostgreSqlIdentifierQuoter.Instance, "log", "Audits",
        [
            new ColumnInfo("EventId", "\"EventId\"", "EventId", "BIGINT", "global::System.Int64", false, true, true, false, false, null, ColumnRole.Identity),
            new ColumnInfo("CreatedAt", "\"CreatedAt\"", "CreatedAt", "TIMESTAMPTZ", "global::System.DateTimeOffset", false, false, false, false, false, null, ColumnRole.Plain),
            new ColumnInfo("EventType", "\"Type\"", "Type", "VARCHAR(50)", "global::System.String", false, false, false, false, false, null, ColumnRole.Plain),
            new ColumnInfo("JsonPayload", "\"JsonPayload\"", "JsonPayload", "BYTEA", "global::System.Byte[]", false, false, false, false, false, null, ColumnRole.Plain)
        ]);

    /// <summary>IdentityOnlyKey: identity key only, no other columns — NonKeyNonIdentities is empty.</summary>
    public static EmitContext PostgreSqlIdentityOnlyKey() => Build(
        SqlProviderKind.PostgreSql, PostgreSqlIdentifierQuoter.Instance, "dbo", "Counters",
        [
            new ColumnInfo("CounterId", "\"CounterId\"", "CounterId", "BIGINT", "global::System.Int64", false, true, true, false, false, null, ColumnRole.Identity)
        ]);

    // ── Builder ───────────────────────────────────────────────────────────────

    private static EmitContext Build(
        SqlProviderKind provider,
        IIdentifierQuoter quoter,
        string schema,
        string table,
        ColumnInfo[] columns)
    {
        var allColumns = ImmutableArray.CreateRange(columns);
        var keysBuilder = ImmutableArray.CreateBuilder<ColumnInfo>();
        var identitiesBuilder = ImmutableArray.CreateBuilder<ColumnInfo>();
        var nonIdentitiesBuilder = ImmutableArray.CreateBuilder<ColumnInfo>();
        var nonKeyNonIdentitiesBuilder = ImmutableArray.CreateBuilder<ColumnInfo>();
        var writablesBuilder = ImmutableArray.CreateBuilder<ColumnInfo>();
        var writableNonKeyNonIdentitiesBuilder = ImmutableArray.CreateBuilder<ColumnInfo>();
        var bulkColumnsBuilder = ImmutableArray.CreateBuilder<ColumnInfo>();
        ColumnInfo? concurrencyToken = null;

        foreach (var col in allColumns)
        {
            if (col.IsKey) keysBuilder.Add(col);
            if (col.IsIdentity) identitiesBuilder.Add(col);
            if (!col.IsIdentity) nonIdentitiesBuilder.Add(col);
            if (col is { IsKey: false, IsIdentity: false }) nonKeyNonIdentitiesBuilder.Add(col);
            if (!col.IsIdentity && !col.IsReadOnly) writablesBuilder.Add(col);
            if (col is { IsKey: false, IsIdentity: false } && !col.IsReadOnly && !col.IsConcurrencyToken) writableNonKeyNonIdentitiesBuilder.Add(col);
            if (col.IsKey || !col.IsReadOnly) bulkColumnsBuilder.Add(col);
            if (col.IsConcurrencyToken) concurrencyToken = col;
        }

        var keys = keysBuilder.ToImmutable();
        var nonIdentities = nonIdentitiesBuilder.ToImmutable();
        var writables = writablesBuilder.ToImmutable();
        var whereByKey = BuildPredicate(keys, col => $"{col.ColumnNameQuoted} = @{col.ParameterName}");
        var joinOn = BuildPredicate(keys, col => $"S.{col.ColumnNameQuoted} = T.{col.ColumnNameQuoted}");

        var identityKeyNames = ImmutableHashSet.CreateRange(
            allColumns.Where(c => c.IsIdentity).Select(c => c.Name));
        var keyNames = ImmutableArray.CreateRange(keys.Select(c => c.Name));

        var profile = ProfileInfo.Empty with
        {
            Schema = schema,
            Table = table,
            Keys = keyNames,
            IdentityKeys = identityKeyNames,
        };

        var schemaTableQuoted = quoter.QuoteSchemaTable(schema, table);
        var factorySuffix = provider == SqlProviderKind.PostgreSql ? "Npgsql" : "Sql";

        return new EmitContext(
            Provider: provider,
            DtoTypeName: $"global::Test.{table}",
            DtoSimpleName: table,
            Namespace: "Test",
            TypeKeyword: "record",
            Accessibility: "public",
            SchemaQuoted: quoter.Quote(schema),
            TableQuoted: quoter.Quote(table),
            SchemaTableQuoted: schemaTableQuoted,
            FactoryClassName: table + factorySuffix,
            Columns: allColumns,
            Keys: keys,
            Identities: identitiesBuilder.ToImmutable(),
            NonIdentities: nonIdentities,
            NonKeyNonIdentities: nonKeyNonIdentitiesBuilder.ToImmutable(),
            Writables: writables,
            WritableNonKeyNonIdentities: writableNonKeyNonIdentitiesBuilder.ToImmutable(),
            BulkColumns: bulkColumnsBuilder.ToImmutable(),
            WhereByKey: whereByKey,
            JoinOn: joinOn,
            Quoter: quoter,
            Profile: profile,
            ConcurrencyToken: concurrencyToken,
            ShouldGeneratePatchClass: false
        );
    }

    private static string BuildPredicate(ImmutableArray<ColumnInfo> cols, Func<ColumnInfo, string> selector)
    {
        if (cols.Length == 0) return string.Empty;
        if (cols.Length == 1) return selector(cols[0]);
        
        return string.Join(" AND ", cols.Select(selector));
    }
}


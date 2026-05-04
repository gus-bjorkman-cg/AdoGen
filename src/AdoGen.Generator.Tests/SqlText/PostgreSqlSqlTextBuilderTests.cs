using AdoGen.Generator.Emitters.PostgreSql;

namespace AdoGen.Generator.Tests.SqlText;

public sealed class PostgreSqlSqlTextBuilderTests
{
    // ── Insert ───────────────────────────────────────────────────────────────

    [Fact]
    public void Insert_ShouldIncludeAllNonIdentityColumns()
    {
        var actual = PostgreSqlSqlTextBuilder.Insert(EmitContextFixtures.PostgreSqlUser());
        actual.Should().Be("""INSERT INTO "public"."Users" ("Id", "Name", "Email") VALUES (@Id, @Name, @Email);""");
    }

    [Fact]
    public void Insert_ShouldProduceCorrectSql_WhenNoIdentityColumn()
    {
        var actual = PostgreSqlSqlTextBuilder.Insert(EmitContextFixtures.PostgreSqlOrder());
        actual.Should().Be("""INSERT INTO "public"."Orders" ("Id", "ProductName") VALUES (@Id, @ProductName);""");
    }

    [Fact]
    public void Insert_ShouldSkipIdentityColumn_WhenTableHasIdentity()
    {
        var actual = PostgreSqlSqlTextBuilder.Insert(EmitContextFixtures.PostgreSqlAuditEvent());
        actual.Should().Be("""INSERT INTO "log"."Audits" ("CreatedAt", "Type", "JsonPayload") VALUES (@CreatedAt, @Type, @JsonPayload);""");
    }

    [Fact]
    public void InsertBatchPrefix_ShouldProduceInsertWithoutValues()
    {
        var actual = PostgreSqlSqlTextBuilder.InsertBatchPrefix(EmitContextFixtures.PostgreSqlUser());
        actual.Should().Be("""INSERT INTO "public"."Users" ("Id", "Name", "Email") VALUES""");
    }

    // ── Update ───────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ShouldSetNonKeyColumnsAndFilterByKey()
    {
        var actual = PostgreSqlSqlTextBuilder.Update(EmitContextFixtures.PostgreSqlUser());
        actual.Should().Be("""UPDATE "public"."Users" SET "Name" = @Name, "Email" = @Email WHERE "Id" = @Id;""");
    }

    [Fact]
    public void Update_ShouldProduceCorrectSql_When_NoIdentityColumn()
    {
        var actual = PostgreSqlSqlTextBuilder.Update(EmitContextFixtures.PostgreSqlOrder());
        actual.Should().Be("""UPDATE "public"."Orders" SET "ProductName" = @ProductName WHERE "Id" = @Id;""");
    }

    [Fact]
    public void Update_ShouldSkipIdentityAndKeyInSetClause_When_TableHasIdentity()
    {
        var actual = PostgreSqlSqlTextBuilder.Update(EmitContextFixtures.PostgreSqlAuditEvent());
        actual.Should().Be("""UPDATE "log"."Audits" SET "CreatedAt" = @CreatedAt, "Type" = @Type, "JsonPayload" = @JsonPayload WHERE "EventId" = @EventId;""");
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_ShouldFilterBySingleKey()
    {
        var actual = PostgreSqlSqlTextBuilder.Delete(EmitContextFixtures.PostgreSqlUser());
        actual.Should().Be("""DELETE FROM "public"."Users" WHERE "Id" = @Id;""");
    }

    // ── Truncate ─────────────────────────────────────────────────────────────

    [Fact]
    public void Truncate_ShouldProduceTruncateStatement()
    {
        var actual = PostgreSqlSqlTextBuilder.Truncate(EmitContextFixtures.PostgreSqlUser());
        actual.Should().Be("""TRUNCATE TABLE "public"."Users";""");
    }

    // ── DeleteBatchTemplate ───────────────────────────────────────────────────

    [Fact]
    public void DeleteBatchTemplate_ShouldProduceDeleteWithInClauseOpener()
    {
        var actual = PostgreSqlSqlTextBuilder.DeleteBatchTemplate(EmitContextFixtures.PostgreSqlUser(), "Id");
        actual.Should().Be("""DELETE FROM "public"."Users" WHERE "Id" IN (""");
    }

    // ── Upsert (ON CONFLICT) ──────────────────────────────────────────────────

    [Fact]
    public void Upsert_ShouldProduceInsertOnConflictDoUpdate()
    {
        var actual = PostgreSqlSqlTextBuilder.Upsert(EmitContextFixtures.PostgreSqlUser());

        actual.Should().Be(
            """
            INSERT INTO "public"."Users" ("Id", "Name", "Email") VALUES (@Id, @Name, @Email) ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "Email" = EXCLUDED."Email";
            """);
    }

    [Fact]
    public void Upsert_ShouldExcludeIdentityKeyFromConflictTarget_WhenKeyIsIdentity()
    {
        var actual = PostgreSqlSqlTextBuilder.Upsert(EmitContextFixtures.PostgreSqlAuditEvent());
        actual.Should().Contain("ON CONFLICT ()");
    }

    // ── CreateTable ───────────────────────────────────────────────────────────

    [Fact]
    public void CreateTable_ShouldIncludeAllColumnsAndPrimaryKeyConstraint()
    {
        var actual = PostgreSqlSqlTextBuilder.CreateTable(EmitContextFixtures.PostgreSqlUser());
        
        actual.Should().Be(
            """
                CREATE TABLE IF NOT EXISTS "public"."Users"(
                    "Id" UUID DEFAULT gen_random_uuid() NOT NULL,
                    "Name" VARCHAR(20) NOT NULL,
                    "Email" VARCHAR(50) NOT NULL
                ,CONSTRAINT "PK_Users" PRIMARY KEY ("Id"));
            """);
    }

    [Fact]
    public void CreateTable_ShouldIncludeGeneratedIdentityClause_WhenColumnIsIdentity()
    {
        var actual = PostgreSqlSqlTextBuilder.CreateTable(EmitContextFixtures.PostgreSqlAuditEvent());
        
        actual.Should().Be(
            """
                CREATE TABLE IF NOT EXISTS "log"."Audits"(
                    "EventId" BIGINT GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                    "CreatedAt" TIMESTAMPTZ NOT NULL,
                    "Type" VARCHAR(50) NOT NULL,
                    "JsonPayload" BYTEA NOT NULL
                ,CONSTRAINT "PK_Audits" PRIMARY KEY ("EventId"));
            """);
    }

    // ── BulkCreateTempTable ───────────────────────────────────────────────────

    [Fact]
    public void BulkCreateTempTable_ShouldIncludeAllColumnsAndOperationColumn()
    {
        var actual = PostgreSqlSqlTextBuilder.BulkCreateTempTable(EmitContextFixtures.PostgreSqlUser(), "adogen_users_tmp");
        
        actual.Should().Be(
            """
                CREATE TEMP TABLE IF NOT EXISTS "adogen_users_tmp"(
                    "Id" UUID NOT NULL,
                    "Name" VARCHAR(20) NOT NULL,
                    "Email" VARCHAR(50) NOT NULL,
                    "operation" CHAR(1) NOT NULL);
            """);
    }

    // ── BulkApply ─────────────────────────────────────────────────────────────

    [Fact]
    public void BulkApply_ShouldIncludeAllDmlBlocks_WhenAllOperationsPresent()
    {
        var actual = PostgreSqlSqlTextBuilder.BulkApply(
            EmitContextFixtures.PostgreSqlUser(), "adogen_users_tmp", "\"public\".\"Users\"");

        actual.Should().Be(
            """
                CREATE INDEX IF NOT EXISTS "ix_adogen_users_tmp_op_keys" ON "adogen_users_tmp" ("operation", "Id");
            
                WITH updated AS (
                    UPDATE "public"."Users" AS T
                        SET "Name" = S."Name",
                            "Email" = S."Email"
                    FROM "adogen_users_tmp" AS S
                    WHERE S."operation" = 'U' AND S."Id" = T."Id"
                    RETURNING 1),
                inserted AS (
                    INSERT INTO "public"."Users" ("Id", "Name", "Email")
                        SELECT S."Id", S."Name", S."Email"
                        FROM "adogen_users_tmp" AS S
                        WHERE S."operation" = 'I'
                    RETURNING 1),
                deleted AS (
                    DELETE FROM "public"."Users" AS T
                    USING "adogen_users_tmp" AS S
                    WHERE S."operation" = 'D' AND S."Id" = T."Id"
                    RETURNING 1)
                SELECT
                    (SELECT COUNT(*) FROM inserted) AS Inserted,
                    (SELECT COUNT(*) FROM updated) AS Updated,
                    (SELECT COUNT(*) FROM deleted) AS Deleted;
            """);
    }

    [Fact]
    public void BulkApply_ShouldUseSelectFalseForUpdateBlock_WhenNoNonKeyNonIdentityColumns()
    {
        var actual = PostgreSqlSqlTextBuilder.BulkApply(
            EmitContextFixtures.PostgreSqlIdentityOnlyKey(), "adogen_counters_tmp", "\"dbo\".\"Counters\"");

        actual.Should().Be(
            """
                CREATE INDEX IF NOT EXISTS "ix_adogen_counters_tmp_op_keys" ON "adogen_counters_tmp" ("operation", "CounterId");

                WITH updated AS (
                    SELECT 1 WHERE false),
                inserted AS (
                    SELECT 1 WHERE false),
                deleted AS (
                    DELETE FROM "dbo"."Counters" AS T
                    USING "adogen_counters_tmp" AS S
                    WHERE S."operation" = 'D' AND S."CounterId" = T."CounterId"
                    RETURNING 1)
                SELECT
                    (SELECT COUNT(*) FROM inserted) AS Inserted,
                    (SELECT COUNT(*) FROM updated) AS Updated,
                    (SELECT COUNT(*) FROM deleted) AS Deleted;
            """);
    }
}


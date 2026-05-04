using AdoGen.Generator.Emitters.SqlServer;

namespace AdoGen.Generator.Tests.SqlText;

/// <summary>
/// Direct string assertions for SqlServerSqlTextBuilder — no Roslyn, no Verify snapshots.
/// These tests decouple SQL correctness from C# emission correctness.
/// </summary>
public sealed class SqlServerSqlTextBuilderTests
{
    // ── Insert ───────────────────────────────────────────────────────────────

    [Fact]
    public void Insert_ShouldIncludeAllNonIdentityColumns()
    {
        var actual = SqlServerSqlTextBuilder.Insert(EmitContextFixtures.SqlServerUser());
        actual.Should().Be("INSERT INTO [dbo].[Users] ([Id], [Name], [Email]) VALUES (@Id, @Name, @Email);");
    }

    [Fact]
    public void Insert_ShouldProduceCorrectSql_WhenNoIdentityColumn()
    {
        var actual = SqlServerSqlTextBuilder.Insert(EmitContextFixtures.SqlServerOrder());
        actual.Should().Be("INSERT INTO [dbo].[Orders] ([Id], [ProductName]) VALUES (@Id, @ProductName);");
    }

    [Fact]
    public void Insert_ShouldSkipIdentityColumn_WhenTableHasIdentity()
    {
        var actual = SqlServerSqlTextBuilder.Insert(EmitContextFixtures.SqlServerAuditEvent());
        actual.Should().Be("INSERT INTO [log].[Audits] ([CreatedAt], [Type], [JsonPayload]) VALUES (@CreatedAt, @Type, @JsonPayload);");
    }

    [Fact]
    public void InsertBatchPrefix_ShouldProduceInsertWithoutValues()
    {
        var actual = SqlServerSqlTextBuilder.InsertBatchPrefix(EmitContextFixtures.SqlServerUser());
        actual.Should().Be("INSERT INTO [dbo].[Users] ([Id], [Name], [Email]) VALUES");
    }

    // ── Update ───────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ShouldSetNonKeyColumnsAndFilterByKey()
    {
        var actual = SqlServerSqlTextBuilder.Update(EmitContextFixtures.SqlServerUser());
        actual.Should().Be("UPDATE [dbo].[Users] SET [Name] = @Name, [Email] = @Email WHERE [Id] = @Id;");
    }

    [Fact]
    public void Update_ShouldProduceCorrectSql_WhenNoIdentityColumn()
    {
        var actual = SqlServerSqlTextBuilder.Update(EmitContextFixtures.SqlServerOrder());
        actual.Should().Be("UPDATE [dbo].[Orders] SET [ProductName] = @ProductName WHERE [Id] = @Id;");
    }

    [Fact]
    public void Update_ShouldSkipIdentityAndKeyInSetClause_WhenTableHasIdentity()
    {
        var actual = SqlServerSqlTextBuilder.Update(EmitContextFixtures.SqlServerAuditEvent());
        actual.Should().Be("UPDATE [log].[Audits] SET [CreatedAt] = @CreatedAt, [Type] = @Type, [JsonPayload] = @JsonPayload WHERE [EventId] = @EventId;");
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public void Delete_ShouldFilterBySingleKey()
    {
        var actual = SqlServerSqlTextBuilder.Delete(EmitContextFixtures.SqlServerUser());
        actual.Should().Be("DELETE FROM [dbo].[Users] WHERE [Id] = @Id;");
    }

    [Fact]
    public void Delete_ShouldFilterByAllKeys_WhenCompositeKey()
    {
        var actual = SqlServerSqlTextBuilder.Delete(EmitContextFixtures.SqlServerCompositeKey());
        actual.Should().Be("DELETE FROM [dbo].[OrderLines] WHERE [OrderId] = @OrderId AND [ProductId] = @ProductId;");
    }

    // ── Truncate ─────────────────────────────────────────────────────────────

    [Fact]
    public void Truncate_ShouldProduceTruncateStatement()
    {
        var actual = SqlServerSqlTextBuilder.Truncate(EmitContextFixtures.SqlServerUser());
        actual.Should().Be("TRUNCATE TABLE [dbo].[Users];");
    }

    // ── DeleteBatchTemplate ───────────────────────────────────────────────────

    [Fact]
    public void DeleteBatchTemplate_ShouldProduceDeleteWithInClauseOpener()
    {
        var actual = SqlServerSqlTextBuilder.DeleteBatchTemplate(EmitContextFixtures.SqlServerUser(), "Id");
        actual.Should().Be("DELETE FROM [dbo].[Users] WHERE [Id] IN (");
    }

    // ── Upsert (MERGE) ────────────────────────────────────────────────────────

    [Fact]
    public void Upsert_ShouldProduceMergeWithOnAndMatchedClauses()
    {
        var actual = SqlServerSqlTextBuilder.Upsert(EmitContextFixtures.SqlServerUser());

        actual.Should().Be(
            "UPDATE [dbo].[Users] SET [Name] = @Name, [Email] = @Email " +
            "WHERE [Id] = @Id; " +
            "IF @@ROWCOUNT = 0 " +
            "INSERT INTO [dbo].[Users] ([Id], [Name], [Email]) " +
            "VALUES (@Id, @Name, @Email);");
    }

    [Fact]
    public void Upsert_ShouldExcludeIdentityKeyFromOnClause_WhenKeyIsIdentity()
    {
        var actual = SqlServerSqlTextBuilder.Upsert(EmitContextFixtures.SqlServerAuditEvent());

        actual.Should().Be(
            "UPDATE [log].[Audits] SET [CreatedAt] = @CreatedAt, [Type] = @Type, [JsonPayload] = @JsonPayload " +
            "WHERE [EventId] = @EventId; " +
            "IF @@ROWCOUNT = 0 " +
            "INSERT INTO [log].[Audits] ([CreatedAt], [Type], [JsonPayload]) " +
            "VALUES (@CreatedAt, @Type, @JsonPayload);");
    }

    [Fact]
    public void Upsert_ShouldIncludeBothKeysInOnClause_WhenCompositeKey()
    {
        var actual = SqlServerSqlTextBuilder.Upsert(EmitContextFixtures.SqlServerCompositeKey());
        
        actual.Should().Be(
            "UPDATE [dbo].[OrderLines] SET [Quantity] = @Quantity " +
            "WHERE [OrderId] = @OrderId AND [ProductId] = @ProductId; " +
            "IF @@ROWCOUNT = 0 " +
            "INSERT INTO [dbo].[OrderLines] ([OrderId], [ProductId], [Quantity]) " +
            "VALUES (@OrderId, @ProductId, @Quantity);"
            );
    }

    // ── CreateTable ───────────────────────────────────────────────────────────

    [Fact]
    public void CreateTable_ShouldIncludeAllColumnsAndPrimaryKeyConstraint()
    {
        var actual = SqlServerSqlTextBuilder.CreateTable(EmitContextFixtures.SqlServerUser());

        actual.Should().Be(
            """
            CREATE TABLE [dbo].[Users](
                        [Id] UNIQUEIDENTIFIER DEFAULT NEWID() NOT NULL,
                        [Name] VARCHAR(20) NOT NULL,
                        [Email] VARCHAR(50) NOT NULL
                    ,CONSTRAINT [PK_Users] PRIMARY KEY ([Id]));
            """);
    }

    [Fact]
    public void CreateTable_ShouldIncludeIdentityClause_WhenColumnIsIdentity()
    {
        var actual = SqlServerSqlTextBuilder.CreateTable(EmitContextFixtures.SqlServerAuditEvent());
        
        actual.Should().Be(
            """
            CREATE TABLE [log].[Audits](
                        [EventId] BIGINT IDENTITY(1,1) NOT NULL,
                        [CreatedAt] DATETIMEOFFSET NOT NULL,
                        [Type] NVARCHAR(50) NOT NULL,
                        [JsonPayload] VARBINARY(8000) NOT NULL
                    ,CONSTRAINT [PK_Audits] PRIMARY KEY ([EventId]));
            """);
    }

    // ── BulkCreateTempTable ───────────────────────────────────────────────────

    [Fact]
    public void BulkCreateTempTable_ShouldIncludeAllColumnsAndOperationColumn()
    {
        var actual = SqlServerSqlTextBuilder.BulkCreateTempTable(EmitContextFixtures.SqlServerUser(), "#AdoGen_User");

        actual.Should().Be(
            """
            CREATE TABLE #AdoGen_User(
                        [Id] UNIQUEIDENTIFIER NOT NULL,
                        [Name] VARCHAR(20) NOT NULL,
                        [Email] VARCHAR(50) NOT NULL,
                        [Operation] CHAR(1) NOT NULL);
            """);
    }

    // ── BulkApply ─────────────────────────────────────────────────────────────

    [Fact]
    public void BulkApply_ShouldIncludeAllDmlBlocks_WhenAllOperationsEnabled()
    {
        var actual = SqlServerSqlTextBuilder.BulkApply(EmitContextFixtures.SqlServerUser(), "#AdoGen_User",
            new BulkApplyOptions(true, true));

        actual.Should().Be(
            """
            BEGIN TRY
                    DECLARE @inserted INT = 0, @updated INT = 0, @deleted INT = 0;
                    CREATE INDEX [IX_AdoGen_Users_Op_Key] ON #AdoGen_User ([Operation], [Id]);
            
                    UPDATE T
                    SET
                        T.[Name] = S.[Name],
                        T.[Email] = S.[Email]
                    FROM [dbo].[Users] AS T
                        JOIN #AdoGen_User AS S ON S.[Id] = T.[Id]
                    WHERE S.[Operation] = 'U';
                    SET @updated = @@ROWCOUNT;
            
                    INSERT INTO [dbo].[Users] ([Id], [Name], [Email])
                    SELECT S.[Id], S.[Name], S.[Email]
                    FROM #AdoGen_User AS S
                    WHERE S.[Operation] = 'I';
                    SET @inserted = @@ROWCOUNT;
            
                    DELETE T
                    FROM [dbo].[Users] AS T
                        JOIN #AdoGen_User AS S ON S.[Id] = T.[Id]
                    WHERE S.[Operation] = 'D';
                    SET @deleted = @@ROWCOUNT;
            
                    SELECT @inserted AS Inserted, @updated AS Updated, @deleted AS Deleted;
            
                    END TRY
                    BEGIN CATCH
                        IF OBJECT_ID('tempdb..#AdoGen_User') IS NOT NULL DROP TABLE #AdoGen_User;
                        THROW;
                    END CATCH;
                    IF OBJECT_ID('tempdb..#AdoGen_User') IS NOT NULL DROP TABLE #AdoGen_User;
            """);
    }

    [Fact]
    public void BulkApply_ShouldOmitUpdateBlock_WhenUpdatesDisabled()
    {
        var actual = SqlServerSqlTextBuilder.BulkApply(EmitContextFixtures.SqlServerUser(), "#AdoGen_User",
            new BulkApplyOptions(true, false));

        actual.Should().Be(
            """
            BEGIN TRY
                    DECLARE @inserted INT = 0, @updated INT = 0, @deleted INT = 0;
                    CREATE INDEX [IX_AdoGen_Users_Op_Key] ON #AdoGen_User ([Operation], [Id]);

                    INSERT INTO [dbo].[Users] ([Id], [Name], [Email])
                    SELECT S.[Id], S.[Name], S.[Email]
                    FROM #AdoGen_User AS S
                    WHERE S.[Operation] = 'I';
                    SET @inserted = @@ROWCOUNT;

                    DELETE T
                    FROM [dbo].[Users] AS T
                        JOIN #AdoGen_User AS S ON S.[Id] = T.[Id]
                    WHERE S.[Operation] = 'D';
                    SET @deleted = @@ROWCOUNT;

                    SELECT @inserted AS Inserted, @updated AS Updated, @deleted AS Deleted;

                    END TRY
                    BEGIN CATCH
                        IF OBJECT_ID('tempdb..#AdoGen_User') IS NOT NULL DROP TABLE #AdoGen_User;
                        THROW;
                    END CATCH;
                    IF OBJECT_ID('tempdb..#AdoGen_User') IS NOT NULL DROP TABLE #AdoGen_User;
            """);
    }

    [Fact]
    public void BulkApply_ShouldOmitInsertBlock_WhenInsertsDisabled()
    {
        var actual = SqlServerSqlTextBuilder.BulkApply(EmitContextFixtures.SqlServerUser(), "#AdoGen_User",
            new BulkApplyOptions(false, true));

        actual.Should().Be(
            """
            BEGIN TRY
                    DECLARE @inserted INT = 0, @updated INT = 0, @deleted INT = 0;
                    CREATE INDEX [IX_AdoGen_Users_Op_Key] ON #AdoGen_User ([Operation], [Id]);

                    UPDATE T
                    SET
                        T.[Name] = S.[Name],
                        T.[Email] = S.[Email]
                    FROM [dbo].[Users] AS T
                        JOIN #AdoGen_User AS S ON S.[Id] = T.[Id]
                    WHERE S.[Operation] = 'U';
                    SET @updated = @@ROWCOUNT;

                    DELETE T
                    FROM [dbo].[Users] AS T
                        JOIN #AdoGen_User AS S ON S.[Id] = T.[Id]
                    WHERE S.[Operation] = 'D';
                    SET @deleted = @@ROWCOUNT;

                    SELECT @inserted AS Inserted, @updated AS Updated, @deleted AS Deleted;

                    END TRY
                    BEGIN CATCH
                        IF OBJECT_ID('tempdb..#AdoGen_User') IS NOT NULL DROP TABLE #AdoGen_User;
                        THROW;
                    END CATCH;
                    IF OBJECT_ID('tempdb..#AdoGen_User') IS NOT NULL DROP TABLE #AdoGen_User;
            """);
    }

    [Fact]
    public void BulkApply_ShouldContainOnlyDeleteBlock_WhenOnlyDeletesEnabled()
    {
        var actual = SqlServerSqlTextBuilder.BulkApply(EmitContextFixtures.SqlServerUser(), "#AdoGen_User",
            new BulkApplyOptions(false, false));

        actual.Should().Be(
            """
            BEGIN TRY
                    DECLARE @inserted INT = 0, @updated INT = 0, @deleted INT = 0;
                    CREATE INDEX [IX_AdoGen_Users_Op_Key] ON #AdoGen_User ([Operation], [Id]);

                    DELETE T
                    FROM [dbo].[Users] AS T
                        JOIN #AdoGen_User AS S ON S.[Id] = T.[Id]
                    WHERE S.[Operation] = 'D';
                    SET @deleted = @@ROWCOUNT;

                    SELECT @inserted AS Inserted, @updated AS Updated, @deleted AS Deleted;

                    END TRY
                    BEGIN CATCH
                        IF OBJECT_ID('tempdb..#AdoGen_User') IS NOT NULL DROP TABLE #AdoGen_User;
                        THROW;
                    END CATCH;
                    IF OBJECT_ID('tempdb..#AdoGen_User') IS NOT NULL DROP TABLE #AdoGen_User;
            """);
    }
}


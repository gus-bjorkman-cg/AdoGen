# Changelog

All notable changes to this project will be documented in this file.

## Release 2.0.0.0 - 2026-06-04

### PostgreSQL support (`AdoGen.PostgreSql`)

- **New package:** `AdoGen.PostgreSql` — full PostgreSQL provider via Npgsql.
- New marker interfaces: `INpgsqlMapper`, `INpgsqlDomainModel`, `INpgsqlBulkModel` (same hierarchy as SQL Server equivalents).
- New `NpgsqlProfile<T>` base class for PostgreSQL-specific column configuration.
- PostgreSQL-specific type shorthands: `Text()`, `Bytea()`, `Varbit(n)` (in addition to shared `VarChar`, `Char`, `Decimal`).
- Bulk operations use `COPY` (PostgreSQL BINARY format) via Npgsql, matching SQL Server's `SqlBulkCopy` performance.
- `UpsertRange` added to both `*Bulk` classes — bulk upsert via the same temp-table mechanism as insert/update.
- Domain operations use `INSERT … RETURNING *` instead of `OUTPUT INSERTED.*`.
- Extension methods on `NpgsqlConnection` mirror the SQL Server API surface exactly.
- Generated helper class uses `*Npgsql` naming (e.g. `UserNpgsql`); bulk class uses `*NpgsqlBulk`.
- A single DTO can implement both `ISql*` and `INpgsql*` interfaces simultaneously, generating separate files per provider.

### Optimistic concurrency (`ConcurrencyToken`)

- **New profile option:** `RuleFor(x => x.Version).ConcurrencyToken()`.
- Supported token types: `int`, `long`, `Guid`.
- Generated `UpdateAsync` and `DeleteAsync` include `AND [Version] = @Version` in the WHERE clause.
- For `int`/`long` tokens: `UpdateAsync` also writes `[Version] = @Version + 1` — token is auto-bumped in the same statement.
- For `Guid` tokens: the caller is responsible for setting a new value before calling `UpdateAsync`.
- When 0 rows are affected, `AdoGenConcurrencyException` is thrown (fully-qualified `global::` prefix to avoid ambiguity in dual-provider projects).
- `UpsertAsync` intentionally does **not** enforce the concurrency check.

### Read-only columns

- **New profile option:** `RuleFor(x => x.CreatedAt).ReadOnly()`.
- Read-only columns are excluded from INSERT, UPDATE, and bulk-write column lists.
- Still included in `CREATE TABLE` DDL (use with `DefaultValue(sqlExpr)`) and read back by the mapper.
- Intended for server-managed columns: computed columns, audit timestamps, database-generated defaults.

### `InsertAndReturnAsync` (RETURNING / OUTPUT single row)

- **New:** `InsertAndReturnAsync<T>` extension method on `SqlConnection` and `NpgsqlConnection`.
  - SQL Server: uses `INSERT … OUTPUT INSERTED.*` to return the inserted row in a single round-trip.
  - PostgreSQL: uses `INSERT … RETURNING *`.
  - Returns the fully-populated `T` including all server-generated values (identity columns, database defaults, concurrency tokens).
- Reuses the existing source-generated `Map(reader)` method — no reflection or runtime IL.
- Single-row only; bulk variant is intentionally out of scope.
- SQL Server limitation: fails with certain triggers that cascade inserts to other tables.
- `ISqlDomainModel<T>` and `INpgsqlDomainModel<T>` include a static abstract `InsertAndReturnAsync`.

### Batching (`SqlBatch` / `NpgsqlBatch`)

- **New:** Extension methods on `SqlBatch` (SQL Server) and `NpgsqlBatch` (PostgreSQL) for typed batch operations.
- Available methods: `batch.Insert(model)`, `batch.Update(model)`, `batch.Delete(model)`, `batch.Upsert(model)`, `batch.InsertAndReturn(model)`.
- Mix AdoGen-managed commands with custom `SqlBatchCommand` / `NpgsqlBatchCommand` instances freely.
- `ISqlDomainModel<T>` / `INpgsqlDomainModel<T>` include static abstract `Add*BatchCommand` methods for each operation.

### `ExistsAsync`

- **New:** `ExistsAsync` for single-key models — `connection.ExistsAsync<User>(userId, ct)`.
- **New:** `ExistsAsync` for composite-key models — `connection.ExistsAsync(model, ct)`.
- Uses `SELECT TOP(1) 1 … WHERE pk = @pk` (SQL Server) / `SELECT 1 … WHERE pk = $1 LIMIT 1` (PostgreSQL).

### Scalar query helpers

- **New:** `QueryScalarAsync<T>` — returns a `List<T?>` of values from the first column.
- **New:** `QueryScalarFirstOrDefaultAsync<T>` — returns the first value from the first column, or `default`.
- Available with no-parameter, single-parameter, and multi-parameter overloads on both `SqlConnection` and `NpgsqlConnection`.

### Patch (partial update)

- **New:** `PatchAsync` extension method on `SqlConnection` and `NpgsqlConnection`.
- The generator emits a `{Model}Patch` class for every `ISqlDomainModel` / `INpgsqlDomainModel`.
- A `{Model}Patch` instance carries only the columns explicitly set via fluent `.With*(value)` calls or property setters. Unset columns are **not** included in the UPDATE statement.
- Returns `0` (no-op) when no columns are set on the patch object.
- Does not participate in optimistic concurrency token checks — targets the row by primary key only.
- Read-only columns are excluded from patch writes.

### `ExecuteAsync`

- **New:** `ExecuteAsync` extension on `SqlConnection` / `NpgsqlConnection` — runs a non-query SQL statement and returns the affected row count.
- Available with no-parameter, single-parameter, and multi-parameter overloads.

## [1.0.0] - 2026-02-23

- Initial stable release.
- Fixed lots of bugs and added tests for special cases and configurations.
- Improved source generation code to be more efficient and support more complex scenarios.

## [0.3.0-alpha] - 2026-02-05

- Preview release fixes & bulk operations.

## [0.2.0-alpha] - 2026-02-05

- Preview release fixes.

## [0.1.0-alpha] - 2026-01-30

- First preview release.

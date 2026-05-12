# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### `InsertAndReturnAsync` (RETURNING / OUTPUT single row)

- **New:** `InsertAndReturnAsync<T>` extension method on `SqlConnection` and `NpgsqlConnection`.
  - SQL Server: uses `INSERT … OUTPUT INSERTED.* VALUES (…)` to return the inserted row in a single round-trip.
  - PostgreSQL: uses `INSERT … VALUES (…) RETURNING *` to return the inserted row.
  - Returns the fully-populated `T` including all server-generated values (identity columns, database defaults, concurrency tokens).
- **New:** `InsertAndReturnAsync` static abstract added to `ISqlDomainModel<T>` and `INpgsqlDomainModel<T>` interfaces (breaking change for any manual implementation of these interfaces — existing generator-produced implementations are updated automatically).
- Reuses the existing source-generated `Map(reader)` method — no new reflection or runtime IL.
- Single-row only; bulk variant is intentionally out of scope.
- SQL Server limitation: fails with certain triggers that cascade inserts to other tables; callers should use `InsertAsync` + a subsequent query in those cases.

## [0.1.0-alpha] - 2026-01-30
* 1st Preview release

## [0.2.0-alpha] - 2026-02-05
* Preview release fixes

## [0.3.0-alpha] - 2026-02-05
* Preview release fixes & bulk operations

## [1.0.0] - 2026-02-23
* Initial stable release. 
* Fixed lots of bugs and added tests for special cases and configurations.
* Improved source generation code to be more efficient and support more complex scenarios.
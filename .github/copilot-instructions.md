# Copilot Instructions for AdoGen

AdoGen is a high-performance, reflection-free, Native AOT–compatible micro-ORM for .NET. All mapping and SQL is produced at compile time by a Roslyn source generator. Two providers are stable: **SQL Server** (`AdoGen.SqlServer`) and **PostgreSQL** (`AdoGen.PostgreSql`).

These rules are **non-negotiable**. If a suggestion conflicts with them, it is wrong.
For build/test workflow, project layout, and operational lessons, see `AGENTS.md`.

---

## 1. Priorities (in order)

1. **Runtime performance** — must be at least as fast as Dapper. Memory allocation is AdoGen's primary edge; never regress it.
2. **Compile-time correctness** — invalid config fails at generation time, never at runtime.
3. **API ergonomics** — only when free of performance cost.
4. **Provider extensibility** — future work. Do not pre-abstract.

Benchmarks (`AdoGen.Benchmarks`) are authoritative. Performance claims require benchmark evidence.

---

## 2. Runtime — Absolute Never-Ever (generated + hand-written)

- No `System.Reflection`
- No `dynamic`, expression trees, or runtime IL/code generation
- No LINQ in hot paths
- No `AddWithValue` — use generated typed factories (e.g. `UserSql.CreateParameterEmail(x)`)
- No exceptions for normal control flow
- Must remain Native AOT compatible

`stackalloc`, `Span<T>`, `ref struct`, and unsafe code are allowed **when benchmark-justified**.

---

## 3. Language & API Surface

- Target framework: **.NET 10** (all projects except the generator)
- Generator targets `netstandard2.0`, C# `latest` via PolySharp
- Nullable reference types enabled
- Public I/O is **async-only**, `Async` suffix mandatory
- Verb-based method names

### CancellationToken (strict)

- Every public async I/O method **must require** an explicit `CancellationToken`
- No defaults, no convenience overloads omitting it
- Callers pass `CancellationToken.None` explicitly when not needed
- Token must propagate to every ADO.NET call: `OpenAsync`, `ExecuteReaderAsync`, `ExecuteNonQueryAsync`, `ReadAsync`, etc.
- Missing `CancellationToken` in a public signature is a design bug.

---

## 4. Provider Boundaries

- SQL Server: `SqlConnection`, `SqlCommand`, `SqlParameter`, `SqlDbType` — confined to `AdoGen.SqlServer`
- PostgreSQL: `NpgsqlConnection`, `NpgsqlCommand`, `NpgsqlParameter`, `NpgsqlDbType` — confined to `AdoGen.PostgreSql`
- **No cross-provider abstractions.** Do not introduce `IDbProvider`, `ISqlDialect`, strategy patterns, or shared base classes spanning providers.
- Prefer duplication over abstraction. Generalization requires a third real provider, measured benchmarks, and proven necessity.
- A single DTO may implement both providers' interfaces; each provider's code is generated independently.

---

## 5. Mapping & Profiles

### Generator activation

- DTO must be `partial`
- DTO implements at least one marker interface:
  - SQL Server: `ISqlMapper`, `ISqlDomainModel`, `ISqlBulkModel`
  - PostgreSQL: `INpgsqlMapper`, `INpgsqlDomainModel`, `INpgsqlBulkModel`

### Profiles

- Exactly **one profile per DTO per provider** (`SqlProfile<T>`, `NpgsqlProfile<T>` are separate)
- A profile is **always required**, even when every member could map by default
- No shared, inherited, or generic profiles

### Mandatory configuration (fail at generation time)

- `string` → length + type explicit: `.VarChar(n)` / `.NVarChar(n)` / `.Char(n)` / `.NChar(n)` (SQL Server); `.VarChar(n)` / `.Text()` / `.Char(n)` / `.Bytea()` / `.Varbit(n)` (PostgreSQL)
- `decimal` → `.Decimal(precision, scale)`
- `Guid`, numeric types, `bool`, `DateTime` → default mappings, no config required
- `Id` is the PK by convention; override with `Key(x => x.MyKey)`
- Nullability inferred from `?`

### Optional column behaviors

- `.ConcurrencyToken()` — `int`/`long` (auto-incremented in UPDATE) or `Guid` (caller sets new value). Adds `AND [Col] = @Col` to UPDATE/DELETE WHERE; throws `global::`-qualified `AdoGenConcurrencyException` on 0 affected rows. **Not** enforced by `UpsertAsync` or `PatchAsync`.
- `.ReadOnly()` — excluded from INSERT/UPDATE/bulk/patch writes; still in DDL and mapper read path. Pair with `.DefaultValue(sqlExpr)` for server-managed columns.

Validation must produce a diagnostic at generation time. Runtime validation is a last resort.

---

## 6. SQL Generation Scope (closed set)

The generator emits SQL **only** for these operations, and only when the DTO implements the corresponding interface and a profile exists:

**Domain (`ISqlDomainModel` / `INpgsqlDomainModel`):**
`CreateTableAsync`, `InsertAsync`, `InsertAndReturnAsync`, `UpdateAsync`, `UpsertAsync`, `DeleteAsync`, `PatchAsync`, `TruncateAsync`, `ExistsAsync`

**Bulk (`ISqlBulkModel` / `INpgsqlBulkModel`):**
`InsertAsync(List<T>)` plus the `*Bulk` class with `AddRange` / `UpdateRange` / `UpsertRange` / `RemoveRange` + `SaveChangesAsync`. SQL Server uses `SqlBulkCopy`; PostgreSQL uses binary `COPY`.

**Batching:** `Insert` / `Update` / `Upsert` / `Delete` / `InsertAndReturn` extensions on `SqlBatch` / `NpgsqlBatch`.

**Patch:** generator emits a `{Model}Patch` class per domain model; only fluent-set columns are written, no concurrency check, read-only columns excluded.

**Not generated:** arbitrary `SELECT`, ad-hoc queries, IQueryable, repository/UoW patterns. Callers write their own SQL and use `QueryAsync` / `QueryFirstOrDefaultAsync` / `QueryScalarAsync` / `QueryScalarFirstOrDefaultAsync` / `ExecuteAsync` with generated parameter factories.

Any attempt to widen this set must fail at generation time with a diagnostic. The limit is intentional.

---

## 7. Parameters

- Type, length, and precision/scale metadata is mandatory and explicit
- No implicit inference, ever
- Parameter creation flows through generated factories or profile config — nothing else
- `AddWithValue` is forbidden in both runtime and generated code

---

## 8. Tests

- xUnit only
- Generator unit tests: in-process Roslyn compilation + Verify snapshots, no Docker
- Integration tests: real databases via Testcontainers (MSSQL + PostgreSQL)
- No mocked ADO.NET, no in-memory providers, no SQL hidden behind helpers
- Snapshot approval alone is **not** validation — integration tests must pass for both providers after any generator/runtime change

Test naming: `Subject_ShouldVerb_WhenCondition` for new tests; match surrounding file style when extending existing suites.

---

## 9. Generator Project (`AdoGen.Generator`)

- Prioritize readability, reusability, maintainability — generator perf matters less than runtime perf
- Reflection is allowed **only inside the generator**, never in emitted code
- Avoid unnecessary allocations during generation
- Prefer simple Roslyn syntax walking; incremental pipelines

---

## 10. Non-Goals

AdoGen does not provide, and will not accept additions for:

- Reflection-based or dynamic mapping
- Repository, Unit-of-Work, or IQueryable patterns
- Convenience APIs that hide allocation or round-trip cost
- Dapper compatibility beyond surface familiarity
- Cross-provider abstractions

---

**Summary:** AdoGen is *predictably fast*, *explicit*, and *boringly correct*. If a suggestion prioritizes elegance, abstraction, or convenience over measured performance, it is wrong.

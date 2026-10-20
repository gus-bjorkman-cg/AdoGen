# AdoGen — Feature Requests & Proposed Changes

These proposals stay inside the project's non-goals (`.github/copilot-instructions.md` §10). There are no cross-provider abstractions, no IQueryable, no repository or unit-of-work patterns, and nothing that adds hidden allocations. Each item lists the motivation, a proposed shape, and its priority:

- **P1** — removes a sharp edge users hit today.
- **P2** — clear value.
- **P3** — nice to have.

The `B#` references point to `BUGS.md`.

---

## A. Compile-time correctness (diagnostics)

The project's priority #2 is "invalid config fails at generation time". Several gaps currently produce either a confusing C# compile error inside a `.g.cs` file or a silent misbehaviour.

| ID  | Proposal                                                                                                                                                                                   | Priority | Related     |
|-----|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------|-------------|
| F1  | **Emit AG009** (currently defined but dead) when an upsert has no non-identity key. Also reword the message to be provider-neutral, since it currently mentions MERGE.                       | P1       | B5          |
| F2  | **New diagnostic: key-only model.** For a Domain DTO where every column is a key, emit an Info or Warning, and generate `UpdateAsync` as a no-op or not at all, with upsert as `ON CONFLICT DO NOTHING` or `IF NOT EXISTS INSERT`. | P1       | B6          |
| F3  | **New diagnostic: unknown chain method.** Report any method in a `RuleFor(...)` chain that the provider's handler does not recognize, instead of silently ignoring it. This would have caught PG `.Char` and `.Varbit`. | P1       | B7          |
| F4  | **New diagnostic: unsupported property type.** Covers `uint`, `ulong`, `ushort`, `sbyte`, `TimeSpan`, `char`, enums without config, and arbitrary classes. Today these emit `VARIANT` or `unknown` DDL. | P1       | B15         |
| F5  | **New diagnostic: duplicate profile.** Reported when more than one `SqlProfile<T>` (or more than one `NpgsqlProfile<T>`) exists for the same T.                                             | P2       | B16         |
| F6  | **New diagnostics: unsupported type shape.** Covers nested DTOs, DTOs that inherit from another DTO, abstract DTOs, and non-sealed DTOs. Alternatively, support them (see F11).             | P2       | B10, B11    |
| F7  | **New diagnostic: invalid column name.** Reported when `.Name("…")` contains characters that are invalid in an identifier. Alternatively, sanitize the C# and parameter names and keep the quoted SQL name. | P2       | B14         |
| F8  | **Robust profile parsing.** Report AG007 instead of crashing on non-lambda `RuleFor` args. Support `this.RuleFor(...)`. Report non-constant `Table`/`Schema` args.                          | P2       | B17         |
| F9  | **Diagnostic test coverage.** Add a generator test per descriptor, AG001–AG013 plus any new ones. Today only AG012 is covered.                                                             | P1       | —           |
| F10 | Fix the diagnostic message text: AG002 is inverted, messages are SQL Server–specific, and the AG013 title is wrong.                                                                        | P2       | B19, B28    |

## B. Mapping & profiles

| ID  | Proposal                                                                                                                                                                                                                       | Priority | Related |
|-----|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------|---------|
| F11 | **`Ignore()` profile rule.** `RuleFor(x => x.FullName).Ignore()` excludes a property from DDL, writes and `Map()`. By convention, get-only computed properties (expression-bodied, no backing field) could also be auto-ignored. | P1       | B13     |
| F12 | **PostgreSQL `.Char(n)` / `.Varbit(n)` handlers.** Both are documented but not implemented.                                                                                                                                    | P1       | B7      |
| F13 | **Order-independent mapping (decided).** Resolve ordinals once per reader on the first row and reuse them for every row. Drop `SequentialAccess` for mapped queries. See BUGS.md B1 for the design. | P1       | B1      |
| F14 | **Enum-as-string option.** Enums already map to their underlying integer type. Add an optional `.AsString(n)` that stores the enum name as varchar, using a generated `switch` (no reflection, no `Enum.Parse`). Also document the current integer mapping. | P3       | —       |
| F15 | **Extra CLR types.** Add `TimeSpan` (`time` / `interval`), `char` on SQL Server (`nchar(1)`), and plain unsigned types. Unsigned types should map to the next larger signed type, as is already done for enums with unsigned underlying types. `DateOnly` and `TimeOnly` are already supported. | P2       | B15     |
| F16 | **Non-sealed and inheritance-friendly DTOs.** Stop forcing `sealed partial` in emitters and mirror the user's declaration modifiers.                                                                                           | P2       | B10     |
| F17 | **Nested DTO support.** Emit the containing type's `partial` chain.                                                                                                                                                            | P3       | B11     |
| F18 | **Respect DTO accessibility in generated types.** The `{Model}Patch` class should match the DTO's accessibility, and `sealed` should be consistent across providers.                                                          | P1       | B12     |
| F19 | **Document or configure pluralization.** The default table name pluralizes (y→ies; s/x/z/ch/sh→es; otherwise s). Document this. Optionally add an assembly-level `[AdoGenNaming(Pluralize = false)]`.                         | P3       | —       |
| F20 | **Struct DTOs** (`readonly record struct`) for read-only mapper models. These are zero-allocation per row for `QueryAsync<T>` into a `List<T>`.                                                                              | P3       | —       |
| F37 | **`DefaultValue` accepts a bare expression.** The generator should emit `DEFAULT {expr}` and tolerate a user-supplied leading `DEFAULT`, so both forms work.                                                                 | P1       | B29     |

## C. Runtime API

| ID  | Proposal                                                                                                                                                                                                                                                              | Priority | Related |
|-----|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------|---------|
| F21 | **Null-safe and convertible scalar.** `ExecuteScalarAsync<T>` should return `default` for null or DBNull. Add explicit numeric conversion via a generated or hand-written switch (`Convert.ToInt32` for int, and so on), or add `ExecuteScalarInt64Async`. Existing `QueryScalarAsync<T>` is reader-based; document which to use. | P1       | B8      |
| F22 | **Streaming query: `IAsyncEnumerable<T> QueryUnbufferedAsync<T>(…, CancellationToken)`.** This avoids materializing large result sets. Must use `[EnumeratorCancellation]`, and the reader must be disposed on early break.                                         | P2       | —       |
| F23 | **Chunking for list operations.** `InsertAsync(List<T>)` and `DeleteAsync(List<TKey>)` should split by the SQL Server 2100-parameter / 1000-row limits and the PG 65 535-parameter limit. At minimum, throw a clear `ArgumentException` before hitting the server.  | P1       | B18     |
| F24 | **Distinguish not-found from stale.** On 0 affected rows with a concurrency token, either expose `AdoGenConcurrencyException.Reason`, or offer `TryUpdateAsync` returning `UpdateResult { Updated, NotFound, Conflict }`. The latter avoids exceptions for control flow (copilot-instructions §2). | P2       | B23     |
| F25 | **Patch concurrency (decided: keep, opt-in).** `WithExpectedVersion(expected)` for int/long tokens (auto-bump), and `WithExpectedVersion(expected, next)` for Guid tokens. No expected token means no check and no throw. See BUGS.md B4 for the full semantics. | P1       | B4      |
| F26 | **Connection lifetime option.** Methods auto-open closed connections but never close them. Either close-if-we-opened (Dapper's behaviour) or document the current behaviour clearly. Changing it is a breaking behaviour change, so do it in a major version.        | P2       | —       |
| F27 | **Provider-prefixed type names (next major).** `AdoGenConcurrencyException`, `BulkApplyResult`, `BulkOp` and `PropertyBuilder` collide when both runtimes are referenced (CS0104). Consider `SqlConcurrencyException` / `NpgsqlConcurrencyException`, or move them into provider-specific namespaces. Until then, document `using` aliases. | P2       | B26     |
| F28 | **Upsert under concurrency (SQL Server).** Add `WITH (UPDLOCK, SERIALIZABLE)` to the UPDATE so concurrent upserts of the same key don't race into PK violations.                                                                                                 | P2       | B24     |

## D. DDL helpers

| ID  | Proposal                                                                                                                                                                                                                                   | Priority |
|-----|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------|
| F29 | **Idempotent `CreateTableAsync` on SQL Server.** PostgreSQL already uses `CREATE TABLE IF NOT EXISTS`. SQL Server should use `IF OBJECT_ID(N'[schema].[table]', N'U') IS NULL CREATE TABLE …` for parity.                                    | P2       |
| F30 | **Schema creation.** Neither provider creates a non-default schema. The test contexts create `log` manually. Options: emit `CREATE SCHEMA IF NOT EXISTS` (PG) and `IF SCHEMA_ID(...) IS NULL EXEC('CREATE SCHEMA ...')` (SQL Server), or document that it is the caller's job. | P3       |
| F31 | **Indexes and unique constraints in profiles.** Add `.Unique()` and `HasIndex(x => x.Email)` that only affect DDL. Low risk, and useful for test databases.                                                                                  | P3       |

## E. Build, release & tests

| ID  | Proposal                                                                                                                                                                                         | Priority | Related |
|-----|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------|---------|
| F32 | **Fix version stamping.** Pass `-p:Version` to Compile (not only Pack), or drop `NoBuild` in Pack. Add a CI assertion that the packed DLL's `AssemblyInformationalVersion` equals the tag.         | P1       | B2      |
| F33 | **Fail the release on empty release notes.** Align `ExtractReleaseNotes` with the Changelog heading format (or vice versa) and fail when it extracts nothing.                                    | P1       | B3      |
| F34 | **Integration tests for untested paths:** `QueryMultiAsync`, Patch on a token-bearing model, `UpsertAsync` on `AuditEvent` (identity key), key-only models, InsertAsync and DeleteAsync at the parameter limits, and `ExecuteScalarAsync` returning NULL. | P1       | B22     |
| F35 | **Incremental-generator cache tests.** Use `GeneratorDriver` with `trackIncrementalGeneratorSteps: true` and assert `IncrementalStepRunReason.Cached` after an unrelated edit.                   | P3       | P10     |
| F36 | **Analyzer-only test for `sealed`/`internal` combinations.** Compile the generated output for `internal`, `public`, `sealed` and non-sealed DTOs and assert there are no errors. This catches B10 and B12 automatically. | P2       | —       |

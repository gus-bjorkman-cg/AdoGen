# AdoGen — Performance Improvement Opportunities

Per `.github/copilot-instructions.md`, benchmarks (`src/AdoGen.Benchmarks`) are authoritative. Every item below is a **hypothesis to benchmark** before merging, and each one includes a suggested benchmark. Items are ordered by expected impact.

Cross-reference: several bug fixes in `BUGS.md` have performance implications. They are flagged here so the fix and its benchmark are designed together.

---

## P1. Redesign mapper ordinal resolution (pairs with bug B1)
**Where:** generated `Map(reader)` (DtoMapperEmitter*)

Today the mapper uses a process-wide static ordinal cache, with a `Volatile.Read` on every row. That is fast but incorrect (B1). Options, in performance order:

| Option                                                                                                            | Per-query cost                  | Per-row cost                    | Correctness                                    |
|-------------------------------------------------------------------------------------------------------------------|---------------------------------|---------------------------------|------------------------------------------------|
| Positional (`reader.GetGuid(0)`, …)                                                                               | 0                               | 0                               | Requires SELECT order = declaration order      |
| Per-query ordinals in a generated `readonly struct {Dto}Ordinals`, passed `in` to `Map(reader, in ordinals)`       | N × `GetOrdinal` (hash lookup)  | 0 (also removes `Volatile.Read`) | Correct for any column order                   |
| Static cache keyed by the reader's schema                                                                         | Schema compare                  | 0                               | Correct, but complex                           |

**Decided (maintainer, 2026-10-20): per-query ordinals, resolved on the first row** (BUGS.md B1). Dapper already pays this cost through its cache key, so AdoGen keeps its allocation lead: the struct lives on the stack. It does require dropping `CommandBehavior.SequentialAccess` for mapped queries, because property-order reads are no longer ascending-ordinal. Measure that change in the same benchmark run.

**Benchmark:** `QueryToList` and `QueryFirstOrDefault`, before and after, for both providers.

## P2. SQL Server plan-cache pollution from count-dependent SQL text
**Where:** generated `InsertAsync(List<T>)` and `DeleteAsync(List<TKey>)` (SQL Server); the composite-key delete

The command text is `… VALUES (@p0,@p1,…),(…)` with a shape that depends on the row count. Every distinct list length compiles and caches a new plan. A service that inserts batches of varying size can fill the plan cache with single-use plans, causing CPU spent on compiles and cache churn. This is the same problem the mandatory Size/Precision rule exists to prevent.

**Options:**
- **Bucketed padding:** round the row count up to a power of two (1, 2, 4, …, 512), and pad with a repeated last row for INSERT (de-duplicated by `SELECT DISTINCT`) or with a repeated key for DELETE, where duplicates are harmless. That gives at most about 10 plans per DTO.
- **DELETE by ids:** use `WHERE [Id] IN (SELECT value FROM OPENJSON(@ids) WITH (value uniqueidentifier '$'))`, which needs one parameter and one plan. Alternatively, use a TVP. A TVP needs a user-defined table type, which could be emitted by `CreateTableAsync`.
- Combine either option with chunking (B18).

**Benchmark:** a `DeleteByIds` benchmark at 10/100/1000 ids, measuring both latency and `sys.dm_exec_cached_plans` count.

## P3. PostgreSQL multi-row insert via `UNNEST` arrays
**Where:** generated PostgreSQL `InsertAsync(List<T>)`

The current shape uses `VALUES ($1,$2,…),(…)`: N×M parameters, a different statement per count (so prepared statements are not reused), and a 65 535-parameter limit.

The alternative is:
```sql
INSERT INTO t (a, b, c) SELECT * FROM UNNEST(@a, @b, @c)
```
That uses one typed array parameter per column, the same SQL text for every count (so it auto-prepares well), and has no parameter limit. The cost is building M arrays. Benchmark against the current shape and against binary `COPY` (already used by bulk) at 10/100/1000 rows.

## P4. Avoid per-parameter string allocation for generated names
**Where:** generated `InsertAsync(List<T>)` and `DeleteAsync(ids)` (both providers)

Each parameter gets a fresh `$"@p{i}"` string, plus `sb.Append($"(@p{i}")`. For 1 000 rows × 4 columns, that is 4 000 small string allocations per call.

**Fix:** use a static, lazily-grown `string[] ParamNames` in the runtime (`"@p0"` … `"@p2099"`), shared by all DTOs. Append it with `sb.Append(ParamNames[i])`. This is zero-allocation after warm-up.

**Benchmark:** the existing `InsertList` benchmark, comparing allocated bytes.

## P5. PostgreSQL `DeleteAsync(ids)` copies the list
**Where:** `DomainOpsEmitterNpgSql`, `Value = ids.ToArray()`

Npgsql accepts `List<T>` directly as an array parameter value, so the copy is unnecessary. Also drop the emitted `using System.Linq;`, which is not needed and conflicts with the "no LINQ" rule.

## P6. `params ReadOnlySpan<TParameter>` overloads for multi-parameter APIs
**Where:** `SqlConnectionExtensions` / `NpgsqlConnectionExtensions`, all `IEnumerable<SqlParameter>` overloads

Callers write `[p1, p2]`. A collection expression that targets `IEnumerable<T>` allocates an array, and `foreach` over `IEnumerable<T>` boxes an enumerator. Adding a `params ReadOnlySpan<SqlParameter>` overload makes the call site allocation-free with C# 13 overload resolution. Keep the `IEnumerable` overload for compatibility.

The `CancellationToken` must stay before `params`. That changes the parameter order compared with today's overloads, so design it carefully, or use a `ReadOnlySpan<T>` (non-params) overload instead.

**Benchmark:** `QueryFirstOrDefault` with two parameters, comparing allocated bytes.

## P7. Patch SQL building allocations
**Where:** generated `PatchAsync` (both providers)

- `sb.Append(separator + "[Col] = @Col")` allocates a concatenated string per set column. Use `sb.Append(separator).Append("[Col] = @Col")`.
- `new StringBuilder("UPDATE … SET ")` starts at the default capacity and regrows. Pre-size it, or build into a pooled `ValueStringBuilder`/`stackalloc char[]` (patch SQL is short).
- Optionally cache the SQL text per mask in a small static array when the column count is ≤ 8.

## P8. `ConfigureAwait(false)` in `SqlDataReaderExtensions` (bug B20)
Without it, every `ReadAsync` continuation under a SynchronizationContext posts back to it. That matters for any consumer that has a context. Zero cost to fix.

## P9. Remove pass-through `async`/`await` wrappers (bug B21)
`SqlConnectionSingleIdExtensions.DeleteAsync` (and one Npgsql equivalent) should return the inner `ValueTask` directly, which saves one state machine per call.

## P10. Generator incremental pipeline does a full re-run on every edit
**Where:** `Pipelines/Discovery.cs` (`CreateDtoCandidates`), `Models/*`

There are two causes:
- The syntax predicate accepts every `class` and `record`, the transform calls `GetDeclaredSymbol`, and the result is `Combine`d with `CompilationProvider`. Any change to the compilation re-runs discovery, validation and emit for all DTOs.
- Pipeline models (`DiscoveryDto`, `ProfileInfo`, `EmitContext`) carry `ISymbol`, `SemanticModel`, `ImmutableArray`, `ImmutableDictionary` and mutable `ParamConfig`. None of these are value-equatable, so the incremental cache never hits (see B27).

**Suggested changes:**
- Narrow the predicate to `TypeDeclarationSyntax { BaseList: not null }` with the `partial` modifier, and to `ClassDeclarationSyntax` with a generic base whose name ends in `Profile`.
- Transform into equatable records that contain only strings, enums and an `EquatableArray<T>`, before any `Combine` with the compilation.
- Drop the `CompilationProvider` combine. Resolve interface symbols per node through `SemanticModel.Compilation` inside the transform.

**Measure:** with a large solution (e.g. 200 DTOs), compare IDE typing latency and `dotnet build` incremental time. Roslyn's `GeneratorDriver` tracking (`IncrementalStepRunReason`) can assert cache hits in generator unit tests.

This is a developer-experience improvement, not a runtime one; the generator README says the generator "should feel invisible".

## P11. Bulk insert-only fast path threshold (pairs with B18)
**Where:** `BulkBatchSql.SaveChangesAsync`

The fast path is chosen by `Items.Count * FieldCount < 2100`. Besides the 1000-row correctness bug:
- The `FieldCount` used includes the `Operation` column, which is not sent in the INSERT. The threshold is therefore conservative by one column per row.
- The crossover point where `SqlBulkCopy` wins over a multi-row INSERT is likely lower than about 500 rows.

Benchmark and expose the threshold as a property, as `BulkBatchNpg.ParameterThreshold` already does.

## P12. `ExistsAsync` boxes the scalar
**Where:** generated `ExistsAsync` (both providers)

`ExecuteScalarAsync` returns a boxed `int` (`1`). This is minor. Using `ExecuteReaderAsync(CommandBehavior.SingleRow | SingleResult)` and `reader.ReadAsync()` avoids the box, but it allocates a reader. **Benchmark before changing**; the current approach may well be faster.

---

## Already good (keep)
- `EnableOptimizedParameterBinding` is set for more than 24 parameters on SQL Server.
- Typed parameter factories with explicit Size, Precision and Scale avoid plan-cache bloat from inferred metadata.
- `SqlBulkCopy` with a temp table for SQL Server, and binary `COPY` for PostgreSQL.
- `ANALYZE` on PostgreSQL temp tables above 1000 rows; an index on the SQL Server temp table at 128 rows or more.
- `ValueTask` everywhere, and no LINQ in runtime hot paths. The one exception is the emitted `using System.Linq` noted in P5.

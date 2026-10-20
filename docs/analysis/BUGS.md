# AdoGen — Bug Report

Findings from a full review of the runtime (`AdoGen.SqlServer`, `AdoGen.PostgreSql`), the generator (`AdoGen.Generator`), the build (`build/Build.cs`), and the published 2.0.0 NuGet package.

**Verification legend**
- ✅ **Reproduced.** The generated code was compiled in a scratch project, or the published artifact was inspected.
- 🔍 **Confirmed by reading the code / generated output.** It was not executed against a database, because Docker was unavailable during the review.
- ⚠️ **Suspected or latent.** The code is fragile but no failure was reproduced.

Severity ranges from **Critical** (silent wrong data or a broken release), through **High** (runtime failure on a supported path) and **Medium** (generator produces uncompilable or invalid output for a plausible input), down to **Low** (cosmetic, misleading, or an edge case).

| #   | Severity | Area                    | Summary                                                                              |
|-----|----------|-------------------------|--------------------------------------------------------------------------------------|
| B1  | Critical | Generator / mapper      | Static ordinal cache maps wrong columns when SELECT order differs between queries    |
| B2  | Critical | Build / release         | Published 2.0.0 DLLs are stamped `0.0.0.0` / `0.0.0-localbuild`                      |
| B3  | Critical | Build / release         | NuGet release notes are always empty                                                 |
| B4  | High     | Generator / Patch       | Patch concurrency clause is overwritten; wrong exception on "not found"              |
| B5  | High     | Generator / PostgreSQL  | `ON CONFLICT ()` for identity-key upsert; AG009 never reported                       |
| B6  | High     | Generator               | Key-only models: `UPDATE … SET  WHERE` / `DO UPDATE SET ;`                           |
| B7  | High     | Generator / PostgreSQL  | `.Char(n)` / `.Varbit(n)` silently ignored                                           |
| B8  | High     | Runtime                 | `ExecuteScalarAsync<T>` throws on NULL/DBNull/numeric mismatch                       |
| B9  | High     | Runtime / SQL Server    | `CreateParameter` with null value → "parameter not supplied"                         |
| B10 | Medium   | Generator               | `sealed` forced onto every DTO (breaks inheritance/abstract)                         |
| B11 | Medium   | Generator               | Nested DTOs generate uncompilable code                                               |
| B12 | Medium   | Generator / Patch       | `{Dto}Patch` always `public` → CS0051/CS0053 on internal DTOs                        |
| B13 | Medium   | Generator               | Computed properties treated as columns; no way to ignore                             |
| B14 | Medium   | Generator               | `.Name("with space")` breaks generated C# and SQL                                    |
| B15 | Medium   | Generator               | Unsupported CLR types emit `VARIANT` / `unknown` DDL without diagnostic              |
| B16 | Medium   | Generator               | Duplicate profiles silently first-wins                                               |
| B17 | Medium   | Generator               | Non-lambda args crash generator; `this.RuleFor` ignored                              |
| B18 | Medium   | Generator / runtime     | No chunking for SQL Server 2100-param / 1000-row limits                              |
| B19 | Low      | Diagnostics             | AG002 message inverted; messages always say "SqlProfile"                             |
| B20 | Low      | Runtime / SQL Server    | `SqlDataReaderExtensions` lacks `ConfigureAwait(false)`                              |
| B21 | Low      | Runtime                 | Pass-through `async`/`await` wrappers in single-id `DeleteAsync`                     |
| B22 | Low      | Runtime / docs          | `QueryMultiAsync` untested; doc example skipped a result set (doc fixed)             |
| B23 | Low      | Generator               | Concurrency exception also thrown when row doesn't exist                             |
| B24 | Low      | Generator / SQL Server  | `UpsertAsync` not race-safe without `UPDLOCK, SERIALIZABLE`                          |
| B25 | Low      | Runtime / bulk          | `AddRange(empty)` marks batch dirty                                                  |
| B26 | Low      | Runtime / API           | Same type names in both providers → CS0104 with both usings                          |
| B27 | Low      | Generator               | Pipeline model "value equality" comment is false                                     |
| B28 | Low      | Analyzer release        | AG013 title mismatch in `AnalyzerReleases.Shipped.md`                                |
| B29 | High     | Generator / DDL / docs  | `DefaultValue(expr)` is emitted verbatim without `DEFAULT`; the documented usage gives invalid DDL |

---

## Critical

### B1. Mapper caches column ordinals in `static` fields → wrong data or exceptions when column order differs between queries 🔍
**Files:**
- `src/AdoGen.Generator/Emitters/SqlServer/DtoMapperEmitterSqlServer.cs`
- `src/AdoGen.Generator/Emitters/PostgreSql/DtoMapperEmitterNpgSql.cs`

Generated output for every mapper looks like this:

```csharp
private static bool IsInitialized;
private static int IdOrdinal;
...
public static Order Map(SqlDataReader reader)
{
    if (!Volatile.Read(ref IsInitialized))
    {
        IdOrdinal = reader.GetOrdinal("Id");
        ...
        Volatile.Write(ref IsInitialized, true);
    }
    return new Order(Id: reader.GetGuid(IdOrdinal), ...);
}
```

The ordinals are resolved **once per process**, from whichever reader happens to call `Map` first. Every later query that maps the same type with a different column order, or with extra or leading columns, reads the wrong ordinals. Examples of such queries:
- `SELECT o.*, u.Name FROM ...`
- `SELECT Version, Id, ...`
- a JOIN that selects another table first
- `OUTPUT INSERTED.*` against a table whose physical column order differs

The result is either:
- an `InvalidCastException` (e.g. `GetGuid` on an `int` column), or
- **silently wrong data**, when two columns share a CLR type. For example, `ProductName` and `Description` could be swapped.

Because the cache is process-wide, the result also depends on which query ran first after startup, so it is non-deterministic.

**Repro (either provider):**
```csharp
await conn.QueryAsync<Order>("SELECT Id, ProductName, UserId, Version FROM Orders", ct);
await conn.QueryAsync<Order>("SELECT Version, UserId, ProductName, Id FROM Orders", ct); // wrong ordinals
```

**Aggravating factor:** every `QueryAsync`/`QueryFirstOrDefaultAsync` uses `CommandBehavior.SequentialAccess`. With SequentialAccess, columns must be read in ascending ordinal order, so the SELECT list must also match property declaration order. This requirement was undocumented; it is now documented in `docs/api-reference.md` → *Runtime behaviour*.

**Decided fix (maintainer, 2026-10-20): resolve ordinals per reader, on the first row.** This is option 1 below. Design notes:
- Interface: keep `static abstract T Map(SqlDataReader)` for manual single-row use, where ordinals are resolved on each call. Add an ordinal-aware overload that the runtime loops use, for example `static abstract TOrdinals GetOrdinals(SqlDataReader)` plus `Map(SqlDataReader, in TOrdinals)`. A generated `readonly struct {Dto}Ordinals` keeps the path allocation-free. If the extra generic parameter is too invasive, an `int[]` costs one small allocation per query, not per row.
- Runtime: `QueryAsync`, `QueryFirstOrDefaultAsync` and the `DbDataReader` extensions call `GetOrdinals` once after the first successful `ReadAsync` (and once per result set for multi-result readers), then reuse the ordinals for every row.
- Remove the static `IsInitialized` / `*Ordinal` fields and the per-row `Volatile.Read`.
- **`SequentialAccess` must go, or the read order must change.** With an arbitrary SELECT order, reading in property order means non-ascending ordinals, which both SqlClient and Npgsql reject under `SequentialAccess`. Dropping it (`CommandBehavior.Default`, still with `SingleResult`/`SingleRow`) is the simplest option. Benchmark it; for normal-width rows the cost is expected to be negligible.
- Tests: add integration tests that map the same DTO from two queries with different column orders, and from a query with extra leading columns, on both providers.
- Docs: once shipped, remove the "Column order matters" warnings in `docs/api-reference.md` (Querying and Runtime behaviour) and the matching AGENTS.md lesson.

**Fix options considered (benchmark before choosing):**
1. Resolve ordinals per reader. Compute them once per `QueryAsync` call, before the read loop, and pass a generated `readonly struct {Dto}Ordinals` to `Map(reader, in ordinals)`. That costs one `GetOrdinal` per column per query, which is what Dapper does via its cache key. There is no per-row cost.
2. Map positionally, with no `GetOrdinal` at all, and document that SELECT order must equal declaration order. Fastest, but brittle.
3. Keep the static cache, but key it on the reader's field names or schema hash. This is more complex.

### B2. Published 2.0.0 assemblies are stamped `0.0.0.0` / `0.0.0-localbuild` ✅
**File:** `build/Build.cs` (Pack target, lines ~135–187)

`Pack` runs with `SetNoBuild(true)` and passes `Version`/`AssemblyVersion`/`FileVersion` as MSBuild properties. Because nothing is recompiled, those properties only affect the `.nuspec`. The DLLs keep the values from `Directory.Build.props` (`0.0.0-localbuild`).

Verified by downloading `adogen.sqlserver.2.0.0.nupkg` from nuget.org. `lib/net10.0/AdoGen.SqlServer.dll` contains `0.0.0.0` and `0.0.0-localbuild+61937e2…`, and no `2.0.0` string.

**Impact:** `AssemblyVersion`, `FileVersion` and `AssemblyInformationalVersion` are all wrong. Users and diagnostics tools cannot tell versions apart at runtime.

**Fix:** pass the version properties to the `Compile` step instead, or drop `NoBuild` in `Pack`.

There is a second, latent problem on the same code path. `numericVersion = version.Split('-', 2)[0] + ".0"` is correct for three-part tags (`v2.0.0` gives `2.0.0.0`). However, a four-part tag such as `v2.0.0.0`, the style the Changelog uses, would produce `2.0.0.0.0`, which is not a valid `AssemblyVersion`. It is harmless today only because of `NoBuild` and three-part tags. Fix it before removing `NoBuild`, e.g. by parsing with `System.Version` and padding to 4 parts.

### B3. Package release notes are always empty ✅
**File:** `build/Build.cs` `ExtractReleaseNotes` (lines ~65–90)

The parser looks for `## [{tagVersion}]`, where `tagVersion` is the tag without the `v` (the `v2.0.0` tag gives `2.0.0`). There are two mismatches:
- **Format:** `Changelog.md` uses `## Release 2.0.0.0 - 2026-06-04`, not `## [x]`.
- **Version:** the Changelog says `2.0.0.0`, while the tag is `2.0.0`.

The extraction returns empty, and the 2.0.0 nuspec has no `<releaseNotes>` element (verified).

**Fix:**
- Use Keep-a-Changelog headings that match the tag exactly (`## [2.0.0] - 2026-06-04`), or make the parser tolerant of both forms and of a trailing `.0`.
- Add a CI check that fails Pack when the notes are empty.

---

## High

### B4. `PatchAsync` concurrency clause is built and then overwritten; it throws `AdoGenConcurrencyException` on "row not found" 🔍
**Files:**
- `src/AdoGen.Generator/Emitters/SqlServer/DomainOpsEmitterSqlServer.cs` (`BuildPatchSrc`, ~line 391)
- `src/AdoGen.Generator/Emitters/PostgreSql/DomainOpsEmitterNpgSql.cs` (~line 346)

Generated code for a DTO with a concurrency token (e.g. `Order`):

```csharp
sb.Append(" WHERE [Id] = @Id");
cmd.CommandText += " AND [Version] = @Version";    // appended to the (empty) CommandText...
cmd.Parameters.Add(OrderSql.CreateParameterVersion(patch.VersionValue));
cmd.CommandText = sb.ToString();                   // ...then overwritten
...
if (affected == 0) throw new global::AdoGen.SqlServer.AdoGenConcurrencyException("dbo.Orders");
```

**Consequences:**
- The token is never checked, which happens to match the docs ("Patch does not support concurrency tokens"). However:
  - `OrderPatch` still exposes a `Version` property and a `WithVersion(...)` method. Users will reasonably believe they get optimistic locking.
  - An unused `@Version` parameter is sent on every call.
  - When the row does not exist, `AdoGenConcurrencyException` is thrown. That is the wrong exception type: it is not a concurrency conflict.
  - The token is never bumped (`Version = Version + 1` is not emitted). Even if the WHERE clause were fixed, int/long tokens would not advance.

**Decided fix (maintainer, 2026-10-20): keep it, as an opt-in check.** Use cases that justify it:
- HTTP `PATCH` with `If-Match` / ETag. The client sends only the changed fields plus the version it last saw.
- Changes computed from a prior read, such as a status transition (`Pending → Approved`) or a counter. Without the check, a concurrent writer's change is silently overwritten.

Without patch support, callers in these cases must fall back to `UpdateAsync`. That needs the full row and writes every column, which defeats the purpose of Patch.

Semantics:
- Replace the non-functional `Version` / `WithVersion(...)` members on `{Dto}Patch` with an explicit **expected** token:
  - `WithExpectedVersion(int|long expected)` for int/long tokens. This emits `AND [Version] = @Version` and adds `[Version] = @Version + 1` to the SET list.
  - `WithExpectedVersion(Guid expected, Guid next)` for Guid tokens. This emits `AND [Version] = @Version` plus `SET [Version] = @VersionNext`. Requiring both values makes a missing new token a compile error, not a runtime one.
- Expected token **not set** gives today's documented behaviour: no check, the row count is returned (0 when not found), and **nothing is thrown**.
- Expected token set and 0 rows affected throws `AdoGenConcurrencyException`, consistent with `UpdateAsync`. The not-found ambiguity is the same as B23.
- Fix the overwrite bug: append the predicate to `sb`, not to `cmd.CommandText`, and only add the parameter when the token is set.
- Both providers. Note that `With{Token}` is part of the generated public API, so this is a breaking rename; call it out in the Changelog.
- Integration tests (both providers) on `Order`: the token matches, the token is stale (throws), no token is set (no throw), and a missing row with no token returns 0.

**Alternatives considered:**
- **Support it:** use `sb.Append(" AND [Version] = @Version")`, emit a `[Version] = @Version + 1` SET item for int/long tokens, and keep the throw.
- **Don't support it:** remove the `Version` members from `{Dto}Patch`, the parameter and the throw, and return the affected row count like non-token DTOs do.

Add integration tests for both providers. The current Patch tests use `User`, which has no token.

### B5. PostgreSQL `UpsertAsync` emits `ON CONFLICT () DO UPDATE` for identity-key models 🔍
**File:** `src/AdoGen.Generator/Emitters/PostgreSql/PostgreSqlSqlTextBuilder.cs` `Upsert` (line ~87)

`FilterNonIdentityKeys` removes the identity key, leaving an empty conflict target. Generated output for the sample `AuditEvent` (`Identity(x => x.EventId); Key(x => x.EventId)`):

```sql
INSERT INTO "log"."Audits" (...) VALUES (...) ON CONFLICT () DO UPDATE SET ...
```

This is a syntax error at runtime. The same applies to the batch `Upsert` and `AddUpsertBatchCommand`.

The SQL Server variant (`UPDATE … WHERE [EventId] = @EventId; IF @@ROWCOUNT = 0 INSERT …`) is valid.

Diagnostic **AG009 `NoUpsertMatchKeys` exists for exactly this case but is never reported.** It has no call sites in the generator, and its message still talks about `MERGE`.

**Fix:**
- Report AG009 and skip Upsert generation for identity-only keys.
- Or, on PostgreSQL, fall back to `ON CONFLICT ("EventId")`, which is valid when the identity value is supplied. Note that with `GENERATED ALWAYS` it is not valid.

Add an integration test that exercises `UpsertAsync` on `AuditEvent`. There is none today.

### B6. Key-only models generate invalid `UPDATE … SET  WHERE` and `DO UPDATE SET ;` ✅
**Files:**
- `SqlServerSqlTextBuilder.Update/Upsert`
- `PostgreSqlSqlTextBuilder.Update/Upsert`

A join-table DTO such as `record UserRole(Guid UserId, Guid RoleId)` with `Key(UserId); Key(RoleId)` produces:

```sql
UPDATE [dbo].[UserRoles] SET  WHERE [UserId] = @UserId AND [RoleId] = @RoleId;
INSERT INTO "public"."UserRoles" (...) ON CONFLICT ("UserId", "RoleId") DO UPDATE SET ;
```

Both are invalid SQL.

The bulk emitter already handles this case correctly (`ON CONFLICT … DO NOTHING`, and no UPDATE). The domain emitter does not.

**Fix:**
- When `WritableNonKeyNonIdentities` is empty, skip `UpdateAsync` and `AddUpdateBatchCommand`, or emit an informational diagnostic.
- For Upsert in the same case, emit `INSERT … ON CONFLICT (...) DO NOTHING` on PostgreSQL, and `IF NOT EXISTS (…) INSERT` on SQL Server.
- `{Dto}Patch` is already skipped when there are 0 patch columns.

### B7. PostgreSQL `.Char(n)` and `.Varbit(n)` are silently ignored ✅
**Files:**
- `src/AdoGen.Generator/Parsing/PostgreSql/ChainMethodHandlerNpgsql.cs`
- `ConfigureChainParser` handler list

Only `Type`, `VarChar`, `Text`, `Bytea` and `Decimal` handlers exist for PostgreSQL. `NpgsqlProfile` exposes `.Char(n)` and `.Varbit(n)`, and both are documented in `api-reference.md` and the 2.0 Changelog. Neither has a handler, so the call is dropped without a diagnostic.

**Repro:** `RuleFor(x => x.Code).Char(3)` on a `string` gives DDL `"Code" TEXT NOT NULL` and `NpgsqlDbType.Text` parameters.

The sample `TestTypeNpgsqlProfile` already hits this: `CharString` and `NCharString` (`.Char(10)` and `.Char(15)`) are emitted as `TEXT`. Tests pass only because the round-trip still works.

The `char` (System.Char) properties map to `CHAR(1)` through the default type map, not through the handler.

**Workaround:** `.Type(NpgsqlDbType.Char).Size(n)`.

**Fix:**
- Add `CharChainHandlerNpgsql` (DbType `Char` + Size) and `VarbitChainHandlerNpgsql`.
- Add an "unknown chain method" diagnostic so future API/handler drift fails loudly (see FEATURES F4).

### B8. `ExecuteScalarAsync<T>` throws on NULL / DBNull results and on numeric type mismatches 🔍
**Files:**
- `src/AdoGen.SqlServer/SqlConnectionExtensions.cs` (lines ~476, 502, 529)
- the equivalent in `src/AdoGen.PostgreSql/NpgsqlConnectionExtensions.cs`

The code is `return (T?)await command.ExecuteScalarAsync(ct)`. For an unconstrained `T`, `T?` on a value type is just `T`, so:
- No rows (`null`) with `T = int` gives a `NullReferenceException`.
- A `DBNull.Value` result (e.g. `SELECT MAX(x)` on an empty table) gives an `InvalidCastException`, for **any** `T`, including `string`.
- `SELECT COUNT_BIG(*)` (long) with `T = int`, or PostgreSQL `count(*)` (bigint) with `T = int`, gives an `InvalidCastException`.

The PostgreSQL case is a likely trap: `ExecuteScalarAsync<int>("SELECT COUNT(*) …")` always fails on Npgsql, yet the api-reference used exactly that example. The doc now has a provider note.

The method's XML doc also says it "returns the number of affected rows", which is a copy-paste error.

**Fix:**
```csharp
var v = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
return v is null or DBNull ? default : (T)v;
```
Document that `T` must match the provider type exactly.

### B9. `SqlCommand.CreateParameter(name, value, type, size)` sends a missing parameter when `value` is null 🔍
**File:** `src/AdoGen.SqlServer/SqlCommandExtensions.cs:136`

`p.SqlValue = value;` with `value == null` makes SqlClient omit the parameter, which fails with *"The parameterized query expects the parameter '@x', which was not supplied"*. The Npgsql counterpart already does `value ?? DBNull.Value`.

**Fix:** `p.Value = value ?? DBNull.Value;`

---

## Medium (generator produces uncompilable or invalid output)

### B10. The generator force-adds `sealed` to every DTO partial ✅
**Files:** every emitter, e.g. `DtoMapperEmitterSqlServer.cs:72` and `DomainOpsEmitterSqlServer.cs:47`

Every emitted partial is `{accessibility} sealed partial {record|class} {Name}`. Because one sealed part seals the whole type:
- A non-sealed DTO with a subclass fails with `CS0509: cannot derive from sealed type`.
- An `abstract partial` DTO fails with `CS0418`.
- Derived classes inherit the marker interface, so AG001 ("must be partial") also fires on the subclass.

**Repro:**
```csharp
public partial class Animal : ISqlMapper { public int Id { get; set; } }
public class Dog : Animal { }
// → AG001 on Dog + CS0509 'Dog': cannot derive from sealed type 'Animal'
```

**Fix:** omit `sealed` from generated partials, since the user's declaration decides. Skip discovery for types whose marker interface is inherited only through a base class. If inheritance is intentionally unsupported, report a clear diagnostic and document it.

### B11. Nested DTOs generate invalid code ✅
A DTO declared inside another type (`static partial class Outer { partial record Inner : ISqlMapper }`) is emitted as a namespace-level `partial record Inner`. That code then fails with `CS0311`.

**Fix:** emit the containing-type chain, or report a "nested types not supported" diagnostic.

### B12. `{Dto}Patch` is always `public` → CS0051/CS0053 for internal DTOs with internal member types ✅
**Files:** `DomainOpsEmitterSqlServer.BuildPatchSrc`, `DomainOpsEmitterNpgSql.BuildPatchSrc`

For example, `internal record Paint(Guid Id, Color Color)` with `internal enum Color` gives *"Inconsistent accessibility: property type 'Color?' is less accessible than property 'PaintPatch.Color'"*. The `{Dto}SqlPatchExtensions` class is also always public.

There is also an inconsistency: the SQL Server patch class is `public partial class`, while the PostgreSQL one is `public sealed partial class`. Its shape therefore depends on which provider "owns" the patch class.

**Fix:** use `ctx.Accessibility` for the patch class and the extensions class, and pick one modifier set.

### B13. Computed / get-only properties are treated as columns ✅
**File:** `src/AdoGen.Generator/Pipelines/DiscoveryValidation.cs:164` (`GetOrderedProperties`)

The only filters are `public` and `!static`. A computed property (`public decimal Total => Qty * Price;`) ends up in DDL, INSERT, UPDATE and the mapper. The mapper then fails with `CS1739: … does not have a parameter named 'Total'`.

There is no way to exclude a property: no `.Ignore()` and no attribute.

The same filter also lets indexers through, and does not walk base-type properties.

**Fix:**
- Exclude properties with no setter or init accessor that are not primary-constructor parameters.
- Exclude indexers (`IsIndexer`).
- Add an `Ignore()` profile method (FEATURES F3).

### B14. `.Name("…")` values are not validated, so names with spaces or special characters break the generated C# ✅
For example, `RuleFor(x => x.Count).Name("Item Count")` produces all of the following:
- `private static int Item CountOrdinal;` → CS1002
- SQL `VALUES (@Item Count)`
- `ParameterCount = "Item Count"`

The column name is reused as the C# identifier and as the SQL parameter name.

**Fix:**
- Derive C# identifiers and SQL parameter names from the **property** name, and use the column name only in quoted SQL identifiers.
- Escape `]` as `]]` in SQL Server and `"` as `""` in PostgreSQL inside quoted identifiers. Check `SqlServerIdentifierQuoter` and `PostgreSqlIdentifierQuoter`.

### B15. Unsupported CLR types silently produce invalid DDL ✅
`uint`, `ulong`, `ushort`, `sbyte` and `TimeSpan` (and any unknown type) map to SqlDbType `Variant` and NpgsqlDbType `Unknown`. The generated DDL is then:
- `[U32] VARIANT NOT NULL` on SQL Server. That is invalid: the type is `SQL_VARIANT`, and it is wrong for an integer anyway.
- `"U32" unknown NOT NULL` on PostgreSQL, which is also invalid.

No diagnostic is raised. The same is true of `char` on SQL Server; PostgreSQL maps `char` to `Char`.

This is inconsistent with enums. `MapDefaultSqlDbType` already maps enums whose underlying type is `ushort`, `uint` or `ulong` to `INT`, `BIGINT` and `DECIMAL`, but plain `ushort`, `uint` and `ulong` properties fall through to `Variant` (`src/AdoGen.Generator/Extensions/RoslynSymbolExtensions.cs:40–81`).

**Fix:**
- Map `TimeSpan` to `TIME` on SQL Server and `INTERVAL` on PostgreSQL.
- Map unsigned types to the next-wider signed type, or reject them.
- For anything else without an explicit `.Type(...)`, report a new "unsupported property type" error.

### B16. Duplicate profiles for the same DTO and provider are silently first-wins ✅
**File:** `src/AdoGen.Generator/Pipelines/Discovery.cs:80–85`

With `DupProfileA : SqlProfile<Dup>` (`VarChar(10)`) and `DupProfileB : SqlProfile<Dup>` (`NVarChar(99)`) both present, the generator picks one by declaration order and reports nothing. The rules say "exactly one profile per DTO per provider".

**Fix:** add a "multiple profiles" error diagnostic.

### B17. Malformed profile syntax crashes the generator or is silently ignored ⚠️
**Files:**
- `Parsing/ProfileInfoCollector.cs:108,128,228`
- `Parsing/ConfigureChainParser.cs:53`

- `Key(...)`, `Identity(...)` and `RuleFor(...)` hard-cast their argument to `LambdaExpressionSyntax`. Passing a method group or a variable (`Key(selector)`) throws `InvalidCastException` inside the generator, which surfaces as CS8785 "generator failed" and **suppresses all generated output**.
- `this.RuleFor(...)`, `this.Table(...)` and similar forms are filtered out (`Expression is IdentifierNameSyntax`) and silently ignored.
- Non-constant `Table(...)` / `Schema(...)` arguments are silently ignored instead of reporting AG007.

**Fix:** pattern-match instead of casting, report AG007 or a new diagnostic for unsupported shapes, and accept the `this.` member-access form.

### B18. SQL Server multi-row `InsertAsync(List<T>)` / `DeleteAsync(ids)` have no chunking, so large lists fail 🔍
**Files:**
- `DomainOpsEmitterSqlServer` (InsertAsync list, DeleteAsync ids)
- `src/AdoGen.SqlServer/BulkBatchSql.cs` (insert-only fast path, ~line 220)

- SQL Server allows at most **2100 parameters** and **1000 rows per `VALUES` table constructor**.
- The public `InsertAsync(List<T>)` builds a single statement:
  - a 4-column model fails above 524 rows (error 8003);
  - a 1–2 column model fails above 1000 rows (error 10738).
- `DeleteAsync(List<TKey>)` fails above 2100 ids, and above 1000 ids because of the VALUES limit.
- `BulkBatchSql`'s insert-only fast path checks only `Items.Count * FieldCount < 2100`. `FieldCount` includes the `Operation` column, so for a model with a single writable column, 1001–1049 rows pass the check and then fail with 10738.
- PostgreSQL `InsertAsync(List<T>)` similarly fails at 65 535 parameters. PostgreSQL `DeleteAsync(ids)` uses `= ANY(@ids)`, which is fine.

**Fix:**
- Chunk inside the generated method, or throw a clear `ArgumentOutOfRangeException` that points to the bulk API.
- Add a row-count (≤ 1000) condition to the bulk fast path.
- The limits are now documented in `docs/api-reference.md`.

---

## Low

### B19. AG002 message is inverted; messages always say "SqlProfile" 🔍
`src/AdoGen.Generator/Diagnostics/SqlDiagnostics.cs:19` reads *"Type '{0}' has SqlProfile, create a class with SqlProfile<{0}>"*. It should say "has **no** profile". It also always says `SqlProfile`, even for PostgreSQL DTOs, and the same applies to the AG003–AG006 texts ("in its SqlProfile"). Include the provider-specific base class name. ✅ Reproduced: `error AG002: Type 'Gadget' has SqlProfile, create a class with SqlProfile<Gadget>`.

**AG003 has a format/argument mismatch and is effectively unreachable.** Its message has four placeholders (`{0}`–`{3}`), but every validator passes two arguments (`dto.Dto.Name, property.Name`). It is chosen only when `cfg is null`. However, `ProfileInfoCollector` (lines 166–168) creates a default `ParamConfig` for every property, so profiled DTOs always get AG004, AG005 or AG006 instead. Either remove AG003, or give it a real trigger and pass all four arguments.

The diagnostic is reported at the **profile's** location (`dto.Profile!.Locations`), not at the offending property. Pointing at the property, or at the `RuleFor` call if one exists, would make the IDE squiggle more useful.

### B20. Missing `ConfigureAwait(false)` in `SqlDataReaderExtensions` 🔍
- `src/AdoGen.SqlServer/SqlDataReaderExtensions.cs` has **zero** `ConfigureAwait(false)` calls; every `ReadAsync` and `NextResultAsync` captures the context.
- The PostgreSQL reader extensions have them.

Library code should not capture the SynchronizationContext. Doing so risks deadlocks in legacy ASP.NET or UI apps that block on async, and it costs extra posts.

### B21. Pass-through `async … => await` wrappers ⚠️
`SqlConnectionSingleIdExtensions.DeleteAsync` overloads (and one in the Npgsql single-id extensions) add a state machine per call. That goes against the AGENTS.md lesson "return the inner ValueTask directly".

### B22. `QueryMultiAsync` is untested, and the documented example skipped a result set 🔍
`reader.QueryAsync<T>()` and `QueryFirstOrDefaultAsync<T>()` already call `NextResultAsync` internally. The old api-reference example also called `await reader.NextResultAsync(ct)` between the two reads, which skips the second result set. It also did not dispose the reader. The doc is now fixed.

The command is disposed (`await using var command`) before the returned reader is consumed. SqlClient tolerates this; Npgsql behaviour should be verified. No integration test covers `QueryMultiAsync` for either provider.

### B23. Concurrency exception also thrown when the row does not exist ⚠️
Generated `UpdateAsync`/`DeleteAsync` for token models throw `AdoGenConcurrencyException` on `affected == 0`. That covers both "stale" and "deleted / never existed", so callers cannot distinguish them. Document it, or check existence on the failure path only.

### B24. SQL Server `UpsertAsync` is not race-safe ⚠️
`UPDATE … WHERE pk; IF @@ROWCOUNT = 0 INSERT …` without `WITH (UPDLOCK, SERIALIZABLE)` (or `HOLDLOCK`) lets two concurrent upserts of the same key both see 0 rows. Both then INSERT, which raises a PK violation.

**Fix:** `UPDATE t WITH (UPDLOCK, SERIALIZABLE) SET … WHERE …`. That is the standard pattern, and its cost is negligible on the happy path.

### B25. Bulk `AddRange(empty)` marks the batch as dirty ⚠️
`HasInserts` and the other flags are set to `true` even when the enumerable is empty. In `SaveChangesAsync` the "open connection if closed" check is effectively dead, because a non-null transaction implies an open connection.

### B26. Provider type names collide when both providers are imported ⚠️
`AdoGenConcurrencyException`, `BulkApplyResult`, `BulkOp` and `PropertyBuilder` exist in both `AdoGen.SqlServer` and `AdoGen.PostgreSql`. A project with `global using` for both namespaces, as `examples/AdoGen.Sample` has, gets CS0104 (ambiguous reference) as soon as user code writes `catch (AdoGenConcurrencyException)`.

The generator side-steps this by emitting `global::`-qualified names. User code needs aliases; the pattern is now documented in api-reference. Consider provider-prefixed names in the next major version.

### B27. Pipeline model equality comment is false ⚠️
`Models/EmitContext.cs` comments claim value equality for incremental caching. However:
- `DiscoveryDto`, `ProfileInfo` and `EmitContext` hold `ISymbol`, `SemanticModel`, `ImmutableArray<T>`, `ImmutableDictionary<,>` and a mutable `ParamConfig`;
- none of these have structural equality.

See PERFORMANCE P10.

### B28. `AnalyzerReleases.Shipped.md` AG013 title mismatch 🔍
The release file says "Multiple concurrency tokens not allowed", while the descriptor title is "Multiple concurrency tokens". This is cosmetic.

---

## Found during the documentation pass (severity per summary table)

### B29. `DefaultValue(expr)` emitted without the `DEFAULT` keyword 🔍
**Files:**
- `src/AdoGen.Generator/Emitters/SqlServer/SqlServerSqlTextBuilder.cs:23`
- `src/AdoGen.Generator/Emitters/PostgreSql/PostgreSqlSqlTextBuilder.cs:24`

The DDL builder appends the expression as-is: `$" {col.DefaultSqlExpression}"`. Before this review, the documentation (api-reference "Property configuration" and "Read-only columns") showed:
```csharp
RuleFor(x => x.CreatedAt).ReadOnly().DefaultValue("GETUTCDATE()");
```
That generates `[CreatedAt] DATETIME2 GETUTCDATE() NOT NULL`, which `CreateTableAsync` rejects with a syntax error. It works only if the caller includes the keyword, as the sample does (`examples/AdoGen.Sample/Features/TestTypes/TestType.cs`: `.DefaultValue("DEFAULT SYSUTCDATETIME()")`). The XML doc on `PropertyBuilder.DefaultValue` ("Sets default value.") doesn't say which form is expected.

**Fix:** have the generator emit `DEFAULT {expr}`, and strip a leading `DEFAULT ` from the user's string for backward compatibility. Also improve the XML doc. Until then, the api-reference documents the `"DEFAULT …"` form.

---

## Checked and OK / not reproduced
- **PostgreSQL `string` without a rule defaults to `text`.** This is by design (PostgreSQL best practice; maintainer decision). `copilot-instructions.md` §5 and the api-reference were updated to match.
- **Interface-kind selection in `Discovery`** (`x.Select(y => y.Kind).FirstOrDefault()`) depends on the order of `AllInterfaces`. A DTO declared `: ISqlMapper, ISqlBulkModel` still generated Bulk in the scratch test. Using `Max()` would make this robust (latent only).
- **PostgreSQL `VarChar(n)`** is matched case-insensitively against the handler name `"Varchar"`, so it works.
- **SQL Server identity-key upsert** (`AuditEvent`) generates valid SQL.
- **Bulk upsert for key-only models** correctly emits `DO NOTHING` on PostgreSQL and skips UPDATE on SQL Server.

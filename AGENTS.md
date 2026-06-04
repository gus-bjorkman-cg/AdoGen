# AGENTS.md — AdoGen Codebase Guide

> **Operational guide for agents.** For the non-negotiable rule set (performance, runtime constraints, provider boundaries, SQL generation scope), read `.github/copilot-instructions.md` first. This file covers *how to work in the repo*, not *what the rules are*.

## What Is AdoGen

AdoGen is a **reflection-free, Native AOT-compatible micro-ORM** for .NET. A Roslyn source generator reads `partial` DTOs + profile classes and emits `*.g.cs` files — no runtime IL, no reflection, no `AddWithValue`. Two stable providers: SQL Server (`AdoGen.SqlServer`) and PostgreSQL (`AdoGen.PostgreSql`).

---

## Project Layout

| Path | Role |
|---|---|
| `src/AdoGen.SqlServer/` | Runtime for SQL Server — extension methods on `SqlConnection`, interfaces, bulk ops |
| `src/AdoGen.PostgreSql/` | Runtime for PostgreSQL — same shape, Npgsql types |
| `src/AdoGen.Generator/` | Roslyn incremental generator (`netstandard2.0`). Pipelines → Emitters |
| `src/AdoGen.Generator.Tests/` | Generator unit tests using in-process Roslyn compilation + Verify snapshots (no Docker needed) |
| `src/AdoGen.SqlServer.Tests/` | Integration tests — real SQL Server via Testcontainers |
| `src/AdoGen.PostgreSql.Tests/` | Integration tests — real PostgreSQL via Testcontainers |
| `src/AdoGen.Benchmarks/` | BenchmarkDotNet project; benchmarks are authoritative on performance |
| `examples/AdoGen.Sample/` | Sample models and profiles used by integration tests |
| `build/Build.cs` | Nuke build script; all CI targets defined here |

---

## Build & Test

```bash
# Build (uses Nuke)
./build.sh

# Or just dotnet
dotnet build

# ⚠️ Always run `dotnet build` before `dotnet test` when generator emitters have changed.
# The VS/dotnet test adapter caches test case serialisations from the previous run.
# If the assembly has not been rebuilt first, the test runner will crash with
# "Catastrophic failure: ArgumentNullException" during test discovery.
# Running `dotnet build` (or the full `./build.sh`) before `dotnet test` avoids this.

# Run generator unit tests (no Docker needed)
# Must run dotnet build first — dotnet test alone will fail with "Catastrophic failure"
# on the first cold run after a generator change.
dotnet build src/AdoGen.Generator.Tests/
dotnet test src/AdoGen.Generator.Tests/ --no-build

# If generator output changed and snapshots need updating:
# 1. Run tests — they will fail and write *.received.txt files to Snapshots/
# 2. Inspect the received files to confirm the changes are intentional
# 3. Replace the verified snapshots:
#    cd src/AdoGen.Generator.Tests/Snapshots
#    for f in *.received.txt; do mv "$f" "${f/.received.txt/.verified.txt}"; done
# 4. Re-run tests to confirm they pass
# 5. Run integration tests to validate the generated code works against a real database
#    (see below) — snapshot approval alone is NOT sufficient validation

# Run integration tests (requires Docker — starts Testcontainers automatically)
# These are the authoritative validation that generated code is correct.
# Always run these after any generator or runtime change.
dotnet test src/AdoGen.SqlServer.Tests/
dotnet test src/AdoGen.PostgreSql.Tests/

# Run benchmarks
dotnet run --project src/AdoGen.Benchmarks -c Release
```

Integration tests spin up a real database container per test collection. `TestContext` creates the container, runs `CreateTableAsync<T>`, and tears it down. Tests inherit from `TestBase` which seeds and truncates data around each test.

---

## Core Patterns

### DTO + Profile → Generated Code

1. Declare a `partial` record implementing one or more marker interfaces per provider:
   - SQL Server: `ISqlMapper`, `ISqlDomainModel`, `ISqlBulkModel`
   - PostgreSQL: `INpgsqlMapper`, `INpgsqlDomainModel`, `INpgsqlBulkModel`
   - A single DTO may implement interfaces for **both** providers simultaneously.
2. Create a provider-specific profile per provider the DTO targets — `SqlProfile<T>` and/or `NpgsqlProfile<T>`. A profile is **always required**, even when every member would map by default.
3. The generator emits separate `*.g.cs` files per provider.

```csharp
public sealed partial record Order(Guid Id, string ProductName, Guid UserId)
    : ISqlDomainModel, INpgsqlDomainModel;

public sealed class OrderProfile : SqlProfile<Order>
{
    public OrderProfile() => RuleFor(x => x.ProductName).VarChar(50);
}

public sealed class OrderNpgsqlProfile : NpgsqlProfile<Order>
{
    public OrderNpgsqlProfile() => RuleFor(x => x.ProductName).VarChar(50);
}
```

For mandatory column configuration, generated SQL scope, concurrency tokens, and read-only columns, see `.github/copilot-instructions.md` §5–§6.

---

## Generator Internals

The generator is an incremental Roslyn source generator in `AdoGen.Generator`:
- `Pipelines/Discovery.cs` — finds eligible types via syntax + semantic analysis
- `Pipelines/DiscoveryValidation.cs` — validates profiles and emits diagnostics
- `Emitters/SqlServer/` and `Emitters/PostgreSql/` — produce the `.g.cs` text per provider
- `Diagnostics/` — all diagnostic descriptors; invalid config → compile error, never runtime error

The generator targets `netstandard2.0`. Modern C# syntax is available via PolySharp. No reflection in emitted code.

---

## Generator Tests

Tests compile source strings in-process using `CSharpCompilation`, run the generator, and verify output with [Verify](https://github.com/VerifyTests/Verify) snapshots stored in `Snapshots/`. No Docker or database required.

To update snapshots after intentional generator changes:
```bash
dotnet test src/AdoGen.Generator.Tests/ -- --verify-update
```

Use `AdoGenType` (e.g. `AdoGenType.SqlBulkModel`) and `TestTypes` (e.g. `TestTypes.User`) as xUnit theory parameters — see `TestHelpers.cs` for the full list.

---


## Agent Working Rules

- After any generator emitter change: `dotnet build` → generator tests → snapshot review → integration tests. Snapshot approval alone is not validation.
- Always `dotnet build src/AdoGen.Generator.Tests/` before `dotnet test` after any generator change. "Catastrophic failure: ArgumentNullException" during test discovery = serialisation bug in `TestHelpers.cs`, not a stale cache.
- When updating snapshots: run tests → inspect `*.received.txt` → `mv *.received.txt → *.verified.txt` → re-run tests → run integration tests.
- Before writing a new test method, check whether one already exists — `replace_string_in_file` can duplicate a method if context overlaps with both old and new content.
- When testing a conditional code path, verify the fixture actually satisfies the condition before writing the assertion.
- Document mistakes here immediately in the format: **RULE: one actionable sentence.** No narrative.

---

## Lessons Learned

- **`new StringBuilder(string)` sets content; `new StringBuilder(int)` sets capacity only.** Use `new StringBuilder(capacity); sb.Append(template);` when pre-sizing. Mixing these silently produces empty output.
- **xUnit deserialization fields on `readonly record struct` must be `static`.** An instance field `_items` causes `ArgumentNullException` on cold test discovery when xUnit constructs a default struct.
- **`[ModuleInitializer]` conflicts with PolySharp in test projects.** PolySharp polyfills `ModuleInitializerAttribute` as a `class` (not `Attribute`), causing `CS0616`. Use a `static` constructor on a `static` class instead — same once-only semantics.
- **MERGE `ON (...)` / `ON CONFLICT (...)` must exclude identity keys.** `EmitContext.JoinOn` covers all keys (correct for bulk JOIN). Filter with `ctx.Keys.Where(col => !col.IsIdentity)` for upsert conflict targets.
- **Raw string literals in emitters embed their indentation into the generated string.** Use `sb.AppendLine(...)` chains for dynamically-assembled SQL; raw string literals only for static outer C# structure.
- **`BuildJoined`-style helpers need an explicit separator parameter.** `", "` and `" AND "` are not interchangeable; always pass the separator explicitly at the call site.
- **`async ValueTask` wrappers that only `await` one call are wasteful.** Return the inner `ValueTask`/`ValueTask<T>` directly.
- **Bogus `StrictMode` requires every property to have a rule.** When adding a new property to a DTO used in Faker-based tests, add a corresponding `.RuleFor(x => x.NewProp, ...)` or tests will throw at runtime.
- **Concurrency token (int/long): same parameter used in both SET and WHERE.** `[Token] = @Token + 1` (SET) and `AND [Token] = @Token` (WHERE) share one parameter. For Guid tokens, only WHERE is augmented; the value is set by the caller as a normal writable column.
- **`AdoGenConcurrencyException` thrown with fully-qualified `global::` prefix** to avoid ambiguity in projects referencing both `AdoGen.SqlServer` and `AdoGen.PostgreSql`.
- **Test method naming: prefer `Subject_ShouldVerb_WhenCondition`** (e.g. `Exists_ShouldReturnTrue_WhenUserExists`) for new tests. The codebase still contains older free-form names (`InsertUser_ShouldInsertUser`); when extending an existing file, match the surrounding style rather than mixing conventions.
- **AAA comments are mandatory when a test has more than one logical step.** Single-action tests (`Act` + `Assert` only) may omit `// Arrange`. Multi-step tests must have all three comments.
- **`SELECT EXISTS`-style queries must use `TOP(1)` (SQL Server) or `LIMIT 1` (PostgreSQL)** to stop the engine from scanning past the first matching row.
- **`InsertAndReturnAsync` uses `Map(reader)` on the DTO partial class, not on the `*Sql`/`*Npgsql` static helper class.** The generated `Map` lives on the DTO itself (from `ISqlMapper<T>`), so call `DtoName.Map(reader)`, not `DtoNameSql.Map(reader)`.

---

## Key Files to Read First

- `.github/copilot-instructions.md` — full non-negotiable rule set
- `src/AdoGen.SqlServer/GeneratorInterfaces.cs` — SQL Server marker interfaces
- `src/AdoGen.PostgreSql/GeneratorInterfaces.cs` — PostgreSQL marker interfaces
- `src/AdoGen.SqlServer/PropertyBuilder.cs` — SQL Server profile fluent API
- `src/AdoGen.PostgreSql/NpgsqlProfile.cs` — PostgreSQL profile fluent API
- `examples/AdoGen.Sample/` — real models and profiles used by all tests (includes dual-provider examples)
- `src/AdoGen.Generator/Pipelines/Discovery.cs` — generator entry point


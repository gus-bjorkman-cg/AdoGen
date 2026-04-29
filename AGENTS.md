# AGENTS.md — AdoGen Codebase Guide

## What Is AdoGen

AdoGen is a **reflection-free, Native AOT-compatible micro-ORM** for .NET that uses Roslyn source generation to produce all mapping and SQL code at compile time. The generator reads `partial` DTOs + profile classes and emits `*.g.cs` files — no runtime IL, no `AddWithValue`, no expression trees.

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

# Run generator unit tests (no Docker needed)
dotnet test src/AdoGen.Generator.Tests/

# Run integration tests (requires Docker — starts Testcontainers automatically)
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
   - A single DTO can implement interfaces for **both** providers simultaneously
2. Create a provider-specific profile subclass for **each** provider the DTO targets:
   - `SqlProfile<T>` for SQL Server
   - `NpgsqlProfile<T>` for PostgreSQL
   - A profile is **always required**, even when all members would map automatically
3. The generator emits separate `*.g.cs` files per provider.

```csharp
// DTO targeting both providers
public sealed partial record Order(Guid Id, string ProductName, Guid UserId)
    : ISqlDomainModel, INpgsqlDomainModel;

// One profile per provider
public sealed class OrderProfile : SqlProfile<Order>
{
    public OrderProfile()
    {
        RuleFor(x => x.ProductName).VarChar(50);
    }
}

public sealed class OrderNpgsqlProfile : NpgsqlProfile<Order>
{
    public OrderNpgsqlProfile()
    {
        RuleFor(x => x.ProductName).VarChar(50);
    }
}
```

### Mandatory Configurations (fail at generation time if missing)
- `string` → must call `.VarChar(n)`, `.NVarChar(n)`, `.Char(n)`, or `.NChar(n)` (SQL Server); `.VarChar(n)` or equivalent (PostgreSQL)
- `decimal` → must call `.Decimal(precision, scale)`
- `Guid`, numeric types, `bool`, `DateTime` → default mappings, no config needed
- `Id` property → treated as PK by convention; override with `Key(x => x.MyKey)`
- **A profile is always required** — even when all members have default mappings

### CancellationToken — Non-Negotiable
Every public async method requires an explicit `CancellationToken`. No overloads that omit it. Callers pass `CancellationToken.None` if cancellation is not needed.

### No `AddWithValue` — Ever
Use generated typed factory methods: `UserSql.CreateParameterId(id)`, `UserSql.CreateParameterEmail(email)`.

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

## Absolute Rules (Runtime Code)

- No `System.Reflection`
- No expression trees, dynamic, or runtime IL
- No LINQ in hot paths
- No `AddWithValue`
- Async-only public I/O APIs with mandatory `CancellationToken`
- SQL Server types (`SqlConnection`, `SqlParameter`, `SqlDbType`) stay in `AdoGen.SqlServer`
- PostgreSQL types (`NpgsqlConnection`, `NpgsqlParameter`, `NpgsqlDbType`) stay in `AdoGen.PostgreSql`
- No cross-provider abstractions

---

## Key Files to Read First

- `.github/copilot-instructions.md` — full non-negotiable rule set
- `src/AdoGen.SqlServer/GeneratorInterfaces.cs` — SQL Server marker interfaces
- `src/AdoGen.PostgreSql/GeneratorInterfaces.cs` — PostgreSQL marker interfaces
- `src/AdoGen.SqlServer/PropertyBuilder.cs` — SQL Server profile fluent API
- `src/AdoGen.PostgreSql/NpgsqlProfile.cs` — PostgreSQL profile fluent API
- `examples/AdoGen.Sample/` — real models and profiles used by all tests (includes dual-provider examples)
- `src/AdoGen.Generator/Pipelines/Discovery.cs` — generator entry point


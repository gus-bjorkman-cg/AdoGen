# AdoGen

**A high‑performance, reflection‑free micro‑ORM for .NET**  
built around source‑generated mappings and explicit parameter metadata.

AdoGen focuses on **predictable performance**, **Native AOT compatibility**,  
and **doing parameter binding correctly** — without magic, reflection, or runtime code generation.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## Motivation

AdoGen started from real frustrations with existing .NET data‑access libraries:

- **Dapper's `CancellationToken` story is awkward.** You can't pass one to async calls without constructing a `CommandDefinition` every time.
- **Dapper's parameter binding is repetitive.** For every query with string parameters you end up redefining `DbType`, `Size`, etc. across the codebase — the same domain knowledge, scattered everywhere.
- **Dapper's bulk performance is slow**, and the bulk extension is paid product.
- **Native AOT broke everything.** When targeting AOT APIs, Dapper's reflection‑based mapping didn't work. There is an AOT‑compatible fork, but at that point the question became: *what if I just build what I actually want?*

The key insight: if you require developers to declare their domain mapping once (string lengths, types, precision), that same metadata can power **parameter creation**, **table creation**, **CRUD generation**, and **bulk operations** — all at compile time, all reflection‑free.

---

## Why AdoGen?

| Goal | How |
|------|-----|
| **Fast by default** | Source‑generated mappers and parameter builders — no reflection, no runtime IL, no `AddWithValue`. |
| **Native AOT ready** | Zero reliance on `System.Reflection` or expression‑tree compilation at runtime. |
| **Explicit parameters** | String lengths, decimal precision, and DB types are declared up front and validated at compile time. |
| **Familiar API** | Query and command extensions on `SqlConnection` / `NpgsqlConnection` — if you know Dapper, you know this. |

---

## Supported Providers

| Provider | Runtime Package | Status |
|----------|-----------------|--------|
| SQL Server | `AdoGen.SqlServer` | ✅ Stable |
| PostgreSQL | `AdoGen.PostgreSql` | ✅ Stable |

---

## Getting Started

### 1. Install packages

AdoGen requires **two** packages: a **runtime package** for your database provider and the **source generator**.

#### SQL Server

```shell
dotnet add package AdoGen.SqlServer
dotnet add package AdoGen.Generator
```

#### PostgreSQL

```shell
dotnet add package AdoGen.PostgreSql
dotnet add package AdoGen.Generator
```

> [!IMPORTANT]
> `AdoGen.Generator` is a compile‑time source generator only.  
> You **must** also install a provider package (`AdoGen.SqlServer` and/or `AdoGen.PostgreSql`) — it contains the runtime types, interfaces, and extension methods that the generated code depends on.

### 2. Define a model

Models must be `partial` and implement one of the marker interfaces from your provider package:

| Interface | What it generates |
|-----------|-------------------|
| `ISqlMapper` / `INpgsqlMapper` | Reader‑to‑object mapper + typed parameter factory methods |
| `ISqlDomainModel` / `INpgsqlDomainModel` | Everything above + CRUD operations + batch delete by ID |
| `ISqlBulkModel` / `INpgsqlBulkModel` | Everything above + bulk insert/update/delete via temp tables |

Each interface inherits from the one above it — pick the one that matches your use case.

```csharp
// Read‑only — generates mapper + parameter helpers only
public sealed partial record UserView(Guid Id, string Name, string Email) : ISqlMapper;

// Full CRUD — generates mapper + Insert, Update, Upsert, Delete, CreateTable, etc.
public sealed partial record Order(Guid Id, string ProductName, Guid UserId) : ISqlDomainModel;

// Bulk — generates everything above + bulk operations via SqlBulkCopy / COPY
public sealed partial record User(Guid Id, string Name, string Email) : ISqlBulkModel;
```

### 3. Create a profile

A profile tells the generator how to bind properties that need explicit metadata.  
Configuration is inspired by [FluentValidation](https://github.com/JeremySkinner/FluentValidation).

```csharp
public sealed class UserProfile : SqlProfile<User>        // SqlProfile  → SQL Server
{                                                          // NpgsqlProfile → PostgreSQL
    public UserProfile()
    {
        RuleFor(x => x.Name).VarChar(20);
        RuleFor(x => x.Email).VarChar(50);
    }
}
```

**Rules:**

- One profile per model per provider.
- String properties **must** be explicitly configured with type and length (`VarChar`, `NVarChar`, `Char`, etc.).
- `decimal` must declare precision and scale: `.Decimal(18, 2)`.
- `Guid` has a default mapping — no configuration needed.
- A property named `Id` is treated as the primary key by convention. Override with `Key(x => x.MyKey)`.
- Custom table name and schema: `Table("MyTable")` / `Schema("myschema")`.
- Invalid or incomplete configuration fails at compile time with a diagnostic error.

### 4. Query

Write your own SQL — AdoGen maps the results via the source‑generated mapper.

```csharp
// List
var users = await connection.QueryAsync<User>(
    "SELECT * FROM Users", ct);

// Single row
var user = await connection.QueryFirstOrDefaultAsync<User>(
    "SELECT TOP(1) * FROM Users WHERE Email = @Email",
    UserSql.CreateParameterEmail("jane@example.com"), ct);
```

`UserSql` is a source‑generated static class with factory methods that create properly typed `SqlParameter` / `NpgsqlParameter` instances — correct `DbType`, `Size`, and all.

#### Multi‑result queries

```csharp
var reader = await connection.QueryMultiAsync(
    "SELECT * FROM Users; SELECT * FROM Orders", ct);

var users  = await reader.QueryAsync<User>(ct);
var orders = await reader.QueryAsync<Order>(ct);
```

### 5. Commands (Insert, Update, Delete, …)

Available when the model implements `ISqlDomainModel` / `INpgsqlDomainModel`:

```csharp
await connection.InsertAsync(order, ct);
await connection.UpdateAsync(order, ct);
await connection.UpsertAsync(order, ct);
await connection.DeleteAsync(order, ct);
```

Insert multiple records in one roundtrip:

```csharp
await connection.InsertAsync(listOfOrders, ct);
```

Delete by a list of IDs (generated for single‑key models):

```csharp
await connection.DeleteAsync<User, Guid>(listOfIds, ct);
```

Create the table from the profile metadata:

```csharp
await connection.CreateTableAsync<Order>(ct);
```

### 6. Bulk operations

Available when the model implements `ISqlBulkModel` / `INpgsqlBulkModel`.  
Bulk operations use temp tables + `SqlBulkCopy` (SQL Server) or `COPY` (PostgreSQL) to handle large datasets efficiently.

```csharp
var bulk = new UserBulk(capacity: 300);    // UserNpgsqlBulk for PostgreSQL
bulk.AddRange(usersToInsert);
bulk.UpdateRange(usersToUpdate);
bulk.RemoveRange(usersToDelete);

await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync(ct);
await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct);
await bulk.SaveChangesAsync(connection, transaction, ct);
await transaction.CommitAsync(ct);
```

### 7. CancellationToken policy

Every public I/O method **requires** a `CancellationToken` — no convenience overloads that omit it.  
If cancellation is not needed, pass `CancellationToken.None` explicitly.

---

## Dual‑Provider Models

A model can target both providers at the same time:

```csharp
public sealed partial record Order(Guid Id, string ProductName, Guid UserId)
    : ISqlDomainModel, INpgsqlDomainModel;

// One profile per provider
public sealed class OrderSqlProfile : SqlProfile<Order>
{
    public OrderSqlProfile()
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

The generator produces separate files for each provider. The correct extension methods resolve based on connection type (`SqlConnection` vs `NpgsqlConnection`).

---

## Advanced Profile Configuration

```csharp
public sealed class AuditEventProfile : SqlProfile<AuditEvent>
{
    public AuditEventProfile()
    {
        Table("Audits");              // custom table name (default: pluralized class name)
        Schema("log");                // custom schema (default: dbo / public)
        Identity(x => x.EventId);    // identity column
        Key(x => x.EventId);         // primary key override (default: Id)

        RuleFor(x => x.EventType).Name("Type").NVarChar(50);  // column name override
        RuleFor(x => x.JsonPayload).VarBinary(8000);
    }
}
```

---

## What Gets Generated

For a model named `User` implementing `ISqlBulkModel`, the generator produces:

| Generated code | Contents |
|----------------|----------|
| `UserMapper.g.cs` | `partial record User` with the `Map(SqlDataReader)` method, plus `static class UserSql` with typed parameter factory methods |
| `UserDomainOps.g.cs` | `partial record User` with `InsertAsync`, `UpdateAsync`, `UpsertAsync`, `DeleteAsync`, `CreateTableAsync`, `TruncateAsync`, and batch delete by ID |
| `UserBulk.g.cs` | `class UserBulk` — bulk operations via temp table + `SqlBulkCopy` |

The mapper file and domain ops extend the model as `partial record`. The SQL helper (`UserSql`) and bulk class (`UserBulk`) are standalone static/regular classes. None of the generated code uses reflection.

---

## Benchmarks

AdoGen is benchmarked against Dapper and EF Core on every release.  
Below is a summary; full results are in the [docs](docs/) folder.

**SQL Server — Highlights**  
_(.NET 10, Apple M4, BenchmarkDotNet v0.15.8)_

| Operation | AdoGen | Dapper | EF Core | AdoGen Alloc | Dapper Alloc | EF Core Alloc |
|-----------|-------:|-------:|--------:|-------------:|-------------:|--------------:|
| QueryFirstOrDefault | 389 µs | 397 µs | 418 µs | 2.82 KB | 6.05 KB | 15.08 KB |
| QueryToList | 38.8 µs | 40.1 µs | 40.0 µs | 453 B | 825 B | 1,705 B |
| Insert | 1.83 ms | 1.99 ms | 2.64 ms | 5.3 KB | 6.48 KB | 20.09 KB |
| Update | 1.73 ms | 1.78 ms | 2.37 ms | 5.17 KB | 6.32 KB | 142.53 KB |
| BulkInsert 1K | 20.9 ms | — | 37.0 ms | 162 KB | — | 6,091 KB |
| BulkInsert 10K | 84.0 ms | — | 337 ms | 1,412 KB | — | 60,924 KB |

📊 **Full results:** [SQL Server](docs/benchmarks-sqlserver.md) · [PostgreSQL](docs/benchmarks-postgresql.md)

---

## Design Principles

1. **Runtime performance is the primary goal.** If it isn't at least as fast as Dapper, it doesn't ship.
2. **Explicit over implicit.** No `AddWithValue`, no inferred types, no hidden allocations.
3. **Compile‑time safety.** Invalid configurations fail during source generation, not at runtime.
4. **No reflection. Ever.** The runtime code path is entirely generated, AOT‑safe, and allocation‑conscious.

---

## License

[MIT](LICENSE)

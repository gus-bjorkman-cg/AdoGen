# AdoGen

**A high‑performance, reflection‑free micro‑ORM for .NET**  
built around source‑generated mappings and explicit parameter metadata.

AdoGen focuses on **predictable performance**, **Native AOT compatibility**,  
and **doing parameter binding correctly** — without magic, reflection, or runtime code generation.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## Supported providers

| Provider   | Package             | Status    |
|------------|---------------------|-----------|
| SQL Server | `AdoGen.SqlServer`  | ✅ Stable |
| PostgreSQL | `AdoGen.PostgreSql` | ✅ Stable |

---

## Install

```shell
# SQL Server
dotnet add package AdoGen.SqlServer
dotnet add package AdoGen.Generator

# PostgreSQL
dotnet add package AdoGen.PostgreSql
dotnet add package AdoGen.Generator
```

> `AdoGen.Generator` is a compile-time source generator. You must also install a provider package.

---

## Quick start

### 1. Define a model

```csharp
public sealed partial record User(Guid Id, string Name, string Email) : ISqlBulkModel, INpgsqlBulkModel;
```

| Interface   | Generates                                                        | SQL Server        | PostgreSQL           |
|-------------|------------------------------------------------------------------|-------------------|----------------------|
| Mapper only | Mapper + typed parameter factories                               | `ISqlMapper`      | `INpgsqlMapper`      |
| Full CRUD   | Everything above + Insert/Update/Upsert/Delete/Patch/CreateTable | `ISqlDomainModel` | `INpgsqlDomainModel` |
| Bulk ops    | Everything above + bulk insert/update/delete                     | `ISqlBulkModel`   | `INpgsqlBulkModel`   |

### 2. Create a profile

One per model per provider — always required.

```csharp
public sealed class UserProfile : SqlProfile<User>
{
    public UserProfile()
    {
        RuleFor(x => x.Name).VarChar(20);
        RuleFor(x => x.Email).VarChar(50);
    }
}
```

Strings and `decimal` must always be explicitly configured. Invalid config fails at compile time.

### 3. Query

```csharp
// List
var users = await connection.QueryAsync<User>("SELECT * FROM Users", ct);

// Single — typed parameter, never AddWithValue
var user = await connection.QueryFirstOrDefaultAsync<User>(
    "SELECT TOP(1) * FROM Users WHERE Email = @Email",
    UserSql.CreateParameterEmail("jane@example.com"), ct);
```

### 4. Commands

```csharp
await connection.InsertAsync(user, ct);
await connection.UpdateAsync(user, ct);
await connection.UpsertAsync(user, ct);
await connection.DeleteAsync(user, ct);

// Insert and return the row with server-generated values
var inserted = await connection.InsertAndReturnAsync(user, ct);

// Partial update — only columns you set are written
var patch = new UserPatch(userId).WithEmail("new@example.com");
await connection.PatchAsync(patch, ct);
```

### 5. Bulk

```csharp
var bulk = new UserBulk(capacity: 500);
bulk.AddRange(toInsert);
bulk.UpdateRange(toUpdate);
bulk.RemoveRange(toDelete);

await bulk.SaveChangesAsync(connection, transaction, ct);
```

---

## Benchmarks

AdoGen's primary advantage is **memory allocation** — consistently the lowest across all benchmark categories.

**SQL Server highlights** _(.NET 10, Apple M4, BenchmarkDotNet v0.15.8)_

| Operation           | AdoGen  | Dapper  | EF Core  | AdoGen Alloc | Dapper Alloc | EF Core Alloc |
|---------------------|--------:|--------:|---------:|-------------:|-------------:|--------------:|
| QueryFirstOrDefault |  389 µs |  397 µs |   418 µs |      2.82 KB |      6.05 KB |      15.08 KB |
| QueryToList         | 38.8 µs | 40.1 µs |  40.0 µs |        453 B |        825 B |       1,705 B |
| Insert              | 1.83 ms | 1.99 ms |  2.64 ms |       5.3 KB |      6.48 KB |      20.09 KB |
| BulkInsert 10K      | 84.0 ms |       — |   337 ms |     1,412 KB |            — |     60,924 KB |

📊 [SQL Server benchmarks](docs/benchmarks-sqlserver.md) · [PostgreSQL benchmarks](docs/benchmarks-postgresql.md)

---

## Documentation

📖 [Full API reference](docs/api-reference.md) — all methods, profile options, concurrency tokens, read-only columns, batching, scalar helpers, and known limitations.

---

## License

[MIT](LICENSE)

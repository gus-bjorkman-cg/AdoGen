# AdoGen API Reference

Complete reference for all AdoGen capabilities, configuration options, and known limitations.

---

## Table of Contents

- [Marker interfaces](#marker-interfaces)
- [Profiles](#profiles)
  - [Profile-level settings](#profile-level-settings)
  - [Property configuration](#property-configuration)
  - [SQL Server types](#sql-server-types)
  - [PostgreSQL types](#postgresql-types)
- [Querying](#querying)
- [Commands](#commands)
- [Patch (partial update)](#patch-partial-update)
- [Delete by IDs](#delete-by-ids)
- [Exists](#exists)
- [Batching](#batching)
- [Bulk operations](#bulk-operations)
- [Scalar & execute helpers](#scalar--execute-helpers)
- [Concurrency tokens](#concurrency-tokens)
- [Read-only columns](#read-only-columns)
- [Generated output](#generated-output)
- [Limitations](#limitations)

---

## Marker interfaces

Models must be `partial` and implement one or more of the following interfaces to trigger code generation.

| SQL Server        | PostgreSQL           | Generates                                                       |
|-------------------|----------------------|-----------------------------------------------------------------|
| `ISqlMapper`      | `INpgsqlMapper`      | Reader mapper (`Map(reader)`) + typed parameter factory methods |
| `ISqlDomainModel` | `INpgsqlDomainModel` | Everything above + CRUD operations + batch commands             |
| `ISqlBulkModel`   | `INpgsqlBulkModel`   | Everything above + bulk insert/update/delete                    |

Each interface inherits from the one above. A single DTO can implement interfaces for both providers simultaneously.

```csharp
// SQL Server only
public sealed partial record Order(Guid Id, string ProductName, Guid UserId) : ISqlDomainModel;

// Dual-provider
public sealed partial record User(Guid Id, string Name, string Email) : ISqlBulkModel, INpgsqlBulkModel;
```

---

## Profiles

A profile is required for every model per provider, even if all properties have default mappings. Configuration is read at compile time by the generator.

```csharp
public sealed class UserProfile : SqlProfile<User>      // or NpgsqlProfile<User>
{
    public UserProfile()
    {
        // profile-level settings and property rules go here
    }
}
```

### Profile-level settings

| Method                  | Default                                    | Notes                                          |
|-------------------------|--------------------------------------------|------------------------------------------------|
| `Table("name")`         | Class name pluralized                      | Custom table name                              |
| `Schema("name")`        | `dbo` (SQL Server) / `public` (PostgreSQL) | Custom schema                                  |
| `Key(x => x.Prop)`      | Property named `Id`                        | Primary key override                           |
| `Identity(x => x.Prop)` | —                                          | Identity / SERIAL column; excluded from INSERT |

### Property configuration

```csharp
RuleFor(x => x.PropertyName)
    .VarChar(50)               // type + size in one call
    .Name("ColumnName")        // optional column name override
    .ReadOnly()                // optional — exclude from writes
    .ConcurrencyToken()        // optional — optimistic concurrency
    .DefaultValue("GETUTCDATE()"); // optional — DDL DEFAULT expression
```

Common modifiers available on all properties:

| Method                       | Notes                                                               |
|------------------------------|---------------------------------------------------------------------|
| `.Name(string)`              | Override DB column name (default: property name)                    |
| `.Nullable()` / `.NotNull()` | Explicit nullability (inferred from `?` by default)                 |
| `.ReadOnly()`                | Excluded from INSERT/UPDATE/bulk writes; included in DDL and mapper |
| `.DefaultValue(sqlExpr)`     | SQL expression for DDL `DEFAULT` clause                             |
| `.ConcurrencyToken()`        | See [Concurrency tokens](#concurrency-tokens)                       |

### SQL Server types

| Shorthand                        | Equivalent            | Use for                      |
|----------------------------------|-----------------------|------------------------------|
| `VarChar(n)`                     | `SqlDbType.VarChar`   | ASCII strings                |
| `NVarChar(n)`                    | `SqlDbType.NVarChar`  | Unicode strings              |
| `Char(n)`                        | `SqlDbType.Char`      | Fixed-length ASCII           |
| `NChar(n)`                       | `SqlDbType.NChar`     | Fixed-length Unicode         |
| `VarBinary(n)`                   | `SqlDbType.VarBinary` | Binary data                  |
| `Decimal(p, s)`                  | `SqlDbType.Decimal`   | Decimal with precision/scale |
| `Type(SqlDbType.X)` + `.Size(n)` | —                     | Escape hatch for any type    |

`Guid`, `bool`, numeric types, `DateTime`, `DateTimeOffset` have default mappings — no configuration needed.  
**Strings and `decimal` always require explicit configuration.**

### PostgreSQL types

| Shorthand                           | Equivalent             | Use for                            |
|-------------------------------------|------------------------|------------------------------------|
| `VarChar(n)`                        | `NpgsqlDbType.Varchar` | Variable-length strings with limit |
| `Text()`                            | `NpgsqlDbType.Text`    | Unbounded strings                  |
| `Char(n)`                           | `NpgsqlDbType.Char`    | Fixed-length strings               |
| `Bytea()`                           | `NpgsqlDbType.Bytea`   | Binary data                        |
| `Varbit(n)`                         | `NpgsqlDbType.Varbit`  | Bit strings                        |
| `Decimal(p, s)`                     | `NpgsqlDbType.Numeric` | Numeric with precision/scale       |
| `Type(NpgsqlDbType.X)` + `.Size(n)` | —                      | Escape hatch for any type          |

---

## Querying

All query methods auto-open the connection if it is not already open.

### `QueryAsync<T>`

Returns `List<T>`. Requires `ISqlMapper` / `INpgsqlMapper`.

```csharp
// No parameters
var users = await connection.QueryAsync<User>("SELECT * FROM Users", ct);

// Single parameter
var orders = await connection.QueryAsync<Order>(
    "SELECT * FROM Orders WHERE UserId = @UserId",
    OrderSql.CreateParameterUserId(userId), ct);

// Multiple parameters
var results = await connection.QueryAsync<Order>(
    "SELECT * FROM Orders WHERE UserId = @UserId AND Status = @Status",
    [OrderSql.CreateParameterUserId(userId), OrderSql.CreateParameterStatus("active")],
    ct);
```

### `QueryFirstOrDefaultAsync<T>`

Returns `T?`. Same overloads as `QueryAsync<T>`.

### `QueryMultiAsync`

Returns the raw `SqlDataReader` / `NpgsqlDataReader` for multi-result-set queries.

```csharp
var reader = await connection.QueryMultiAsync(
    "SELECT * FROM Users; SELECT * FROM Orders", ct);

var users  = await reader.QueryAsync<User>(ct);
await reader.NextResultAsync(ct);
var orders = await reader.QueryAsync<Order>(ct);
```

All three query methods accept an optional `CommandType`, `SqlTransaction`, and `commandTimeout`.

---

## Commands

Available when the model implements `ISqlDomainModel` / `INpgsqlDomainModel`.

| Method                            | Returns             | Notes                                                  |
|-----------------------------------|---------------------|--------------------------------------------------------|
| `InsertAsync(model, ct)`          | `int` rows affected |                                                        |
| `InsertAsync(List<T>, ct)`        | `int` rows affected | Multi-row, one round-trip                              |
| `InsertAndReturnAsync(model, ct)` | `T`                 | Returns populated row; see [limitations](#limitations) |
| `UpdateAsync(model, ct)`          | `int` rows affected | Throws `AdoGenConcurrencyException` if token mismatch  |
| `UpsertAsync(model, ct)`          | `int` rows affected | Does not enforce concurrency token                     |
| `DeleteAsync(model, ct)`          | `int` rows affected | Throws `AdoGenConcurrencyException` if token mismatch  |
| `TruncateAsync<T>(ct)`            | `int` rows affected |                                                        |
| `CreateTableAsync<T>(ct)`         | `void`              | DDL — creates the table from profile metadata          |

All methods accept an optional `SqlTransaction` / `NpgsqlTransaction` and `commandTimeout`.

`InsertAndReturnAsync` internals:
- SQL Server: `INSERT … OUTPUT INSERTED.*`
- PostgreSQL: `INSERT … RETURNING *`

---

## Patch (partial update)

The generator emits a `{Model}Patch` class for every `ISqlDomainModel` / `INpgsqlDomainModel`. A patch carries only the columns you explicitly set — unset columns are **not** included in the UPDATE statement.

```csharp
// Only Email is updated; Name is unchanged in the database
var patch = new UserPatch(userId).WithEmail("new@example.com");
int affected = await connection.PatchAsync(patch, ct);

// Fluent or property-setter style — both work
var patch2 = new UserPatch(userId) { Email = "new@example.com" };

// Returns 0 (no-op) when no columns are set
var noop = new UserPatch(userId);
int affected = await connection.PatchAsync(noop, ct);   // 0
```

`PatchAsync` accepts an optional transaction and `commandTimeout`.

**Limitation:** Patch does not support concurrency tokens — it targets the row by primary key only.

---

## Delete by IDs

### Single-key models

Deletes a list of records in one round-trip using an `IN (…)` clause.

```csharp
// Explicit key type
await connection.DeleteAsync<User, Guid>(listOfIds, ct);

// Convenience overloads for common key types (Guid, long, int, short, decimal, string)
await connection.DeleteAsync<User>(listOfGuids, ct);
```

### Composite-key models

Uses a VALUES + JOIN pattern.

```csharp
await connection.DeleteAsync(listOfModels, ct);
```

---

## Exists

### Single-key models

```csharp
bool exists = await connection.ExistsAsync<User>(userId, ct);

// Explicit key type
bool exists = await connection.ExistsAsync<User, Guid>(userId, ct);
```

### Composite-key models

```csharp
bool exists = await connection.ExistsAsync(model, ct);
```

Uses `SELECT TOP(1) 1 … WHERE pk = @pk` (SQL Server) / `SELECT 1 … WHERE pk = $1 LIMIT 1` (PostgreSQL).

---

## Batching

Batch multiple typed operations into a single round-trip. Available on `SqlBatch` (SQL Server) and `NpgsqlBatch` (PostgreSQL).

```csharp
await using var batch = connection.CreateBatch();

batch.Insert(order1);
batch.Update(order2);
batch.Delete(order3);
batch.Upsert(order4);

await batch.ExecuteNonQueryAsync(ct);
```

**`InsertAndReturn` in a batch** — use `ExecuteReaderAsync` to read back server-generated values:

```csharp
batch.InsertAndReturn(newOrder1);
batch.InsertAndReturn(newOrder2);

await using var reader = await batch.ExecuteReaderAsync(ct);

var first  = Order.Map(reader);        // first result set
await reader.NextResultAsync(ct);
var second = Order.Map(reader);        // second result set
```

You can freely mix AdoGen-managed commands with custom `SqlBatchCommand` / `NpgsqlBatchCommand` instances in the same batch.

---

## Bulk operations

Available when the model implements `ISqlBulkModel` / `INpgsqlBulkModel`.

```csharp
var bulk = new UserBulk(capacity: 500);          // UserNpgsqlBulk for PostgreSQL
bulk.AddRange(usersToInsert);
bulk.UpdateRange(usersToUpdate);
bulk.UpsertRange(usersToUpsert);
bulk.RemoveRange(usersToDelete);

await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync(ct);
await using var tx = (SqlTransaction)await connection.BeginTransactionAsync(ct);

BulkApplyResult result = await bulk.SaveChangesAsync(connection, tx, ct);
Console.WriteLine($"Inserted: {result.Inserted}, Updated: {result.Updated}, Deleted: {result.Deleted}");

await tx.CommitAsync(ct);
```

SQL Server uses `SqlBulkCopy` via a temp table. PostgreSQL uses `COPY` (binary format) via Npgsql.

---

## Scalar & execute helpers

### `ExecuteAsync`

Runs a non-query statement and returns affected row count.

```csharp
int rows = await connection.ExecuteAsync("DELETE FROM Logs WHERE CreatedAt < @cutoff", param, ct);
```

### `ExecuteScalarAsync<T>`

Returns a single scalar value.

```csharp
int count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users", ct);
```

### `QueryScalarAsync<T>`

Returns `List<T?>` — all values from the first column.

```csharp
var ids = await connection.QueryScalarAsync<Guid>("SELECT Id FROM Users WHERE Active = 1", ct);
```

### `QueryScalarFirstOrDefaultAsync<T>`

Returns the first value from the first column, or `default(T)`.

```csharp
var name = await connection.QueryScalarFirstOrDefaultAsync<string>(
    "SELECT Name FROM Users WHERE Id = @Id",
    UserSql.CreateParameterId(userId), ct);
```

All scalar/execute methods have three overloads: no parameters, single `SqlParameter` / `NpgsqlParameter`, and `IEnumerable<SqlParameter>` / `IEnumerable<NpgsqlParameter>`.

---

## Concurrency tokens

Mark one column per model as the optimistic concurrency token:

```csharp
RuleFor(x => x.Version).ConcurrencyToken();   // int, long, or Guid
```

**Behaviour:**

| Token type     | UPDATE WHERE clause        | UPDATE SET side-effect                                    |
|----------------|----------------------------|-----------------------------------------------------------|
| `int` / `long` | `AND [Version] = @Version` | `[Version] = @Version + 1` (auto-bump)                    |
| `Guid`         | `AND [Version] = @Version` | None — caller sets new value before calling `UpdateAsync` |

- `DeleteAsync` also adds `AND [Version] = @Version` to the WHERE clause.
- If 0 rows are affected, `AdoGenConcurrencyException` is thrown.
- `UpsertAsync` does **not** enforce the concurrency token — use `UpdateAsync` if you need optimistic locking.
- `PatchAsync` does **not** enforce the concurrency token.

---

## Read-only columns

```csharp
RuleFor(x => x.CreatedAt).ReadOnly().DefaultValue("GETUTCDATE()");
```

- Excluded from INSERT, UPDATE, `PatchAsync`, and bulk-write column lists.
- Still emitted in `CREATE TABLE` DDL.
- Still read back by the generated `Map(reader)` method.

Typical use cases: database-generated timestamps, computed columns, audit fields.

---

## Generated output

For a model named `User` implementing `ISqlBulkModel`:

| File                 | Contents                                                                                                                                                                                                                                                   |
|----------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `UserMapper.g.cs`    | `partial record User` with `static T Map(SqlDataReader)` + `static class UserSql` with `CreateParameter*` factory methods                                                                                                                                  |
| `UserDomainOps.g.cs` | `partial record User` with `InsertAsync`, `InsertAsync(List<T>)`, `InsertAndReturnAsync`, `UpdateAsync`, `UpsertAsync`, `DeleteAsync`, `TruncateAsync`, `CreateTableAsync`, `ExistsAsync`, `Add*BatchCommand`; also generates `UserPatch` and `PatchAsync` |
| `UserBulk.g.cs`      | `class UserBulk` — bulk add/update/remove + `SaveChangesAsync`                                                                                                                                                                                             |

For PostgreSQL, the helper class is `UserNpgsql`, bulk class is `UserNpgsqlBulk`.

None of the generated code uses reflection, expression trees, or dynamic IL.

---

## Limitations

- **No arbitrary SELECT generation.** AdoGen never generates arbitrary SELECT queries. `ExistsAsync` is the one exception — it emits a `SELECT TOP(1) 1` / `SELECT 1 … LIMIT 1` query. All other reads use hand-written SQL via `QueryAsync` / `QueryFirstOrDefaultAsync`.
- **`InsertAndReturnAsync` fails with certain SQL Server triggers** that cascade inserts to other tables (`OUTPUT INSERTED.*` is not supported in that scenario). Use `InsertAsync` + a subsequent `QueryFirstOrDefaultAsync` instead.
- **`PatchAsync` targets by primary key only** — it does not participate in optimistic concurrency.
- **Bulk operations require an explicit transaction.** `SaveChangesAsync` does not create one internally.
- **One concurrency token per model.** Only one property may be marked `.ConcurrencyToken()`.
- **Profiles are always required.** Even if every property has a default mapping, a profile class must exist in the compilation.
- **String and `decimal` properties must always be explicitly configured** — missing configuration is a compile-time error.


# AdoGen Diagnostics

The AdoGen source generator reports configuration problems as **compile-time diagnostics**. Invalid configuration never reaches runtime.

All IDs are prefixed `AG`. Descriptors live in `src/AdoGen.Generator/Diagnostics/SqlDiagnostics.cs`, and release tracking is in `src/AdoGen.Generator/AnalyzerReleases.Shipped.md`.

| ID                                          | Severity | Title                                                | Provider |
|---------------------------------------------|----------|------------------------------------------------------|----------|
| [AG001](#ag001--type-must-be-partial)       | Error    | Type must be partial                                 | Both     |
| [AG002](#ag002--missing-profile)            | Error    | Missing SqlProfile                                   | Both     |
| [AG003](#ag003--missing-required-parameter-configuration) | Error | Missing required parameter configuration        | Both     |
| [AG004](#ag004--string-property-requires-explicit-type-and-size) | Error | String property requires explicit SqlDbType and Size | Both |
| [AG005](#ag005--decimal-property-requires-precision-and-scale) | Error | Decimal property requires Precision and Scale | Both |
| [AG006](#ag006--binary-property-requires-explicit-type-and-size) | Error | Binary property requires explicit SqlDbType and Size | Both |
| [AG007](#ag007--non-constant-configuration-argument) | Error | Non-constant configuration argument           | Both     |
| [AG008](#ag008--missing-key-configuration)  | Error    | Missing key configuration                            | Both     |
| [AG009](#ag009--upsert-cannot-be-generated) | Warning  | Upsert cannot be generated                           | —        |
| [AG010](#ag010--static-types-not-supported) | Error    | Static types not supported                           | Both     |
| [AG011](#ag011--invalid-type-visibility)    | Error    | Invalid type visibility                              | Both     |
| [AG012](#ag012--concurrencytoken-unsupported-type) | Error | ConcurrencyToken unsupported type               | Both     |
| [AG013](#ag013--multiple-concurrency-tokens) | Error   | Multiple concurrency tokens                          | Both     |

> **Message text caveat:** messages currently say `SqlProfile` even for PostgreSQL DTOs. Read it as "the provider's profile" (`SqlProfile<T>` or `NpgsqlProfile<T>`). See `docs/analysis/BUGS.md` (B19).

> **Not everything is diagnosed yet.** Some invalid inputs still surface as ordinary C# errors in `*.g.cs` files, or as invalid DDL, instead of an `AG` diagnostic. See [Problems that are not diagnosed yet](#problems-that-are-not-diagnosed-yet).

---

## AG001 — Type must be partial

**Trigger:** a type implements a marker interface (`ISqlMapper`, `ISqlDomainModel`, `ISqlBulkModel`, `INpgsql*`) but is not declared `partial`.

**Fix:**
```csharp
public sealed partial record User(Guid Id, string Name) : ISqlDomainModel;
//            ^^^^^^^
```

## AG002 — Missing profile

**Trigger:** a Domain or Bulk DTO (`ISqlDomainModel` / `ISqlBulkModel` / `INpgsqlDomainModel` / `INpgsqlBulkModel`) has no profile for that provider. Mapper-only DTOs are exempt.

The message currently reads *"Type 'X' has SqlProfile, create a class with SqlProfile<X>"*. It means the profile is **missing**.

**Fix:** add a profile, even an empty one:
```csharp
public sealed class UserProfile : SqlProfile<User> { }          // SQL Server
public sealed class UserNpgsqlProfile : NpgsqlProfile<User> { } // PostgreSQL
```

## AG003 — Missing required parameter configuration

**Trigger:** reserved for a property that has no configuration at all. In practice the generator creates a default configuration for every property, so you will see AG004, AG005 or AG006 instead.

**Fix:** same as AG004–AG006.

## AG004 — String property requires explicit type and size

**Trigger:**
- **SQL Server:** a `string` property without both a type and a size.
- **PostgreSQL:** a `string` property configured as `VarChar` without a size. A `string` with no rule defaults to `text` by design and is not reported.

**Fix:**
```csharp
RuleFor(x => x.Name).NVarChar(100);  // SQL Server: VarChar / NVarChar / Char / NChar
RuleFor(x => x.Name).VarChar(100);   // PostgreSQL: VarChar(n) / Text()
```

**Why:** on SQL Server, without an explicit size ADO.NET infers the parameter size from each value. That creates a new query plan per distinct length (plan-cache bloat), and the Unicode-ness of the type decides whether an index seek is possible. On PostgreSQL, `varchar` and `text` are binary-coercible, so `text` is the recommended default.

## AG005 — Decimal property requires precision and scale

**Trigger:** a `decimal` property without `Decimal(precision, scale)`.

**Fix:**
```csharp
RuleFor(x => x.Price).Decimal(18, 2);
```

## AG006 — Binary property requires explicit type and size

**Trigger:**
- **SQL Server:** a `byte[]` property without `VarBinary(n)` (or `Type(...)` plus `Size(...)`).
- **PostgreSQL:** a `byte[]` property whose type is not `Bytea`. The default is `bytea`, so this only fires if you configured another type.

**Fix:**
```csharp
RuleFor(x => x.Payload).VarBinary(200); // SQL Server
RuleFor(x => x.Payload).Bytea();        // PostgreSQL
```

## AG007 — Non-constant configuration argument

**Trigger:** a profile method argument (size, precision, name, default value, …) is not a compile-time constant. The generator reads profiles from syntax and never runs them, so it can only see literals and `const` values.

**Fix:**
```csharp
private const int NameLength = 100;
RuleFor(x => x.Name).NVarChar(NameLength);  // OK — const
RuleFor(x => x.Name).NVarChar(GetLength()); // AG007
```

> Non-constant `Table(...)` / `Schema(...)` arguments are currently **ignored silently** rather than reported (BUGS.md B17).

## AG008 — Missing key configuration

**Trigger:** a Domain or Bulk DTO has no property named `Id` and no `Key(...)` in its profile. Update, Delete and Upsert need a key.

**Fix:**
```csharp
Key(x => x.OrderNumber);              // single key
Key(x => x.UserId); Key(x => x.RoleId); // composite key
```

## AG009 — Upsert cannot be generated

**Status:** defined but **never reported** by the current generator (BUGS.md B5). It is intended for a model whose only key columns are `Identity(...)`. Such a model has no column to match on, so upsert is meaningless. Today the PostgreSQL generator emits invalid SQL (`ON CONFLICT ()`) for this case instead.

**Workaround:** don't call `UpsertAsync` on identity-keyed models. Use `InsertAsync` or `UpdateAsync`.

## AG010 — Static types not supported

**Trigger:** a `static` class implements a marker interface. (In practice C# prevents static classes from implementing interfaces, so this is a defensive check.)

## AG011 — Invalid type visibility

**Trigger:** the DTO is `private`, `protected`, `private protected` or `protected internal`. Only `public` and `internal` are supported.

> Nested DTOs are not supported even when `public` (BUGS.md B11). Declare DTOs at namespace level.

## AG012 — ConcurrencyToken unsupported type

**Trigger:** `.ConcurrencyToken()` is applied to a property that is not a non-nullable `int`, `long` or `Guid`. Nullable tokens (`int?`, `Guid?`) are rejected.

**Fix:** use `int` or `long` (auto-incremented on update) or `Guid` (caller supplies a new value on each update).

## AG013 — Multiple concurrency tokens

**Trigger:** more than one property in a profile is marked `.ConcurrencyToken()`.

**Fix:** keep exactly one.

---

## Problems that are not diagnosed yet

These inputs are accepted by the generator but produce broken output. They are tracked in `docs/analysis/BUGS.md` and proposed as new diagnostics in `docs/analysis/FEATURES.md` (section A).

| Input                                                                  | Symptom                                                    | Bug |
|------------------------------------------------------------------------|------------------------------------------------------------|-----|
| DTO declared inside another type                                       | CS0311 in generated code                                   | B11 |
| DTO that is `abstract` or used as a base class                         | CS0509 / `sealed` conflict in generated code               | B10 |
| Computed get-only property (e.g. `FullName => …`)                      | CS1739 in generated code                                   | B13 |
| `.Name("Has Space")` or other non-identifier column names              | Invalid C# identifiers / SQL parameter names               | B14 |
| `uint`, `ulong`, `ushort`, `sbyte`, `TimeSpan` (and `char` on SQL Server) | `VARIANT` / `unknown` column type in DDL                | B15 |
| Two profiles for the same DTO and provider                             | First one wins silently                                    | B16 |
| `RuleFor(SomeMethod())` / `this.RuleFor(...)`                          | Generator crash / rule ignored                             | B17 |
| PostgreSQL `.Char(n)` / `.Varbit(n)`                                   | Ignored; column becomes `text`                             | B7  |
| Model whose every column is a key                                      | Invalid UPDATE / UPSERT SQL                                | B6  |
| `DefaultValue("GETUTCDATE()")` without the `DEFAULT` keyword           | Invalid `CREATE TABLE` DDL                                 | B29 |

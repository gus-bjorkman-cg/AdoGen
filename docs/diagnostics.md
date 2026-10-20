# AdoGen Diagnostics

The AdoGen source generator reports configuration problems as **compile-time diagnostics**. Invalid configuration never reaches runtime.

All IDs are prefixed `AG`. Descriptors live in `src/AdoGen.Generator/Diagnostics/SqlDiagnostics.cs`, and release tracking is in `src/AdoGen.Generator/AnalyzerReleases.{Shipped,Unshipped}.md`.

| ID                                          | Severity | Title                                                | Provider |
|---------------------------------------------|----------|------------------------------------------------------|----------|
| [AG001](#ag001--type-must-be-partial)       | Error    | Type must be partial                                 | Both     |
| [AG002](#ag002--missing-profile)            | Error    | Missing profile                                      | Both     |
| [AG004](#ag004--string-property-requires-explicit-type-and-size) | Error | String property requires explicit SqlDbType and Size | Both |
| [AG005](#ag005--decimal-property-requires-precision-and-scale) | Error | Decimal property requires Precision and Scale | Both |
| [AG006](#ag006--binary-property-requires-explicit-type-and-size) | Error | Binary property requires explicit SqlDbType and Size | Both |
| [AG007](#ag007--non-constant-configuration-argument) | Error | Non-constant configuration argument           | Both     |
| [AG008](#ag008--missing-key-configuration)  | Error    | Missing key configuration                            | Both     |
| [AG010](#ag010--static-types-not-supported) | Error    | Static types not supported                           | Both     |
| [AG011](#ag011--invalid-type-visibility)    | Error    | Invalid type visibility                              | Both     |
| [AG012](#ag012--concurrencytoken-unsupported-type) | Error | ConcurrencyToken unsupported type               | Both     |
| [AG013](#ag013--multiple-concurrency-tokens) | Error   | Multiple concurrency tokens                          | Both     |
| [AG014](#ag014--abstract-types-not-supported) | Error  | Abstract types not supported                         | Both     |
| [AG015](#ag015--nested-types-not-supported) | Error    | Nested types not supported                           | Both     |
| [AG016](#ag016--invalid-column-name)        | Error    | Invalid column name                                  | Both     |
| [AG017](#ag017--unsupported-property-type)  | Error    | Unsupported property type                            | Both     |
| [AG018](#ag018--multiple-profiles-for-the-same-type) | Error | Multiple profiles for the same type           | Both     |
| [AG019](#ag019--unsupported-profile-expression) | Error | Unsupported profile expression                     | Both     |

**Removed rules:** AG003 (*Missing required parameter configuration*) was never reported in practice — AG004–AG006 cover the same cases. AG009 (*Upsert cannot be generated*) was never reported; identity-keyed models now get a working upsert (SQL Server: insert-first + `IF @@ROWCOUNT = 0 UPDATE`; PostgreSQL: `MERGE`).

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

Message: *"Type 'X' has no SqlProfile. Create a class deriving from SqlProfile<X>."* (`NpgsqlProfile` for PostgreSQL).

**Fix:** add a profile, even an empty one:
```csharp
public sealed class UserProfile : SqlProfile<User> { }          // SQL Server
public sealed class UserNpgsqlProfile : NpgsqlProfile<User> { } // PostgreSQL
```

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

Non-constant `Table(...)` / `Schema(...)` arguments are reported as well.

## AG008 — Missing key configuration

**Trigger:** a Domain or Bulk DTO has no property named `Id` and no `Key(...)` in its profile. Update, Delete and Upsert need a key.

**Fix:**
```csharp
Key(x => x.OrderNumber);              // single key
Key(x => x.UserId); Key(x => x.RoleId); // composite key
```

## AG010 — Static types not supported

**Trigger:** a `static` class implements a marker interface. (In practice C# prevents static classes from implementing interfaces, so this is a defensive check.)

## AG011 — Invalid type visibility

**Trigger:** the DTO is `private`, `protected`, `private protected` or `protected internal`. Only `public` and `internal` are supported.

Nested DTOs are reported by [AG015](#ag015--nested-types-not-supported).

## AG012 — ConcurrencyToken unsupported type

**Trigger:** `.ConcurrencyToken()` is applied to a property that is not a non-nullable `int`, `long` or `Guid`. Nullable tokens (`int?`, `Guid?`) are rejected.

**Fix:** use `int` or `long` (auto-incremented on update) or `Guid` (caller supplies a new value on each update).

## AG013 — Multiple concurrency tokens

**Trigger:** more than one property in a profile is marked `.ConcurrencyToken()`.

**Fix:** keep exactly one.

## AG014 — Abstract types not supported

**Trigger:** an `abstract` class implements a marker interface. The generated mapper and `InsertAndReturnAsync` must construct the type.

**Fix:** put the marker interface on a concrete type. Markers inherited from a base class are ignored, so a non-sealed DTO may have subclasses without them being treated as DTOs.

## AG015 — Nested types not supported

**Trigger:** the DTO is declared inside another type. Generated extension classes must be top-level.

**Fix:** declare the DTO at namespace level.

## AG016 — Invalid column name

**Trigger:** `.Name("...")` is not a plain identifier (`[A-Za-z_][A-Za-z0-9_]*`). The name is used both as the column name and the SQL parameter name.

**Fix:**
```csharp
RuleFor(x => x.Count).Name("ItemCount");   // OK
RuleFor(x => x.Count).Name("Item Count");  // AG016
```

## AG017 — Unsupported property type

**Trigger:** a property on a profiled (Domain/Bulk) DTO has a CLR type with no default database mapping — e.g. `uint`, `ulong`, `ushort`, `sbyte`, `char` on SQL Server, or an arbitrary class — and the profile doesn't set an explicit type.

**Fix:** set an explicit type (`.Type(SqlDbType.BigInt)` / `.Type(NpgsqlDbType.Bigint)`) or change the property type. `TimeSpan` maps to `TIME` (SQL Server) / `interval` (PostgreSQL) by default.

## AG018 — Multiple profiles for the same type

**Trigger:** more than one `SqlProfile<T>` (or more than one `NpgsqlProfile<T>`) exists for the same DTO.

**Fix:** keep exactly one profile per DTO per provider.

## AG019 — Unsupported profile expression

**Trigger:** `RuleFor`/`Key`/`Identity` is given something other than an inline lambda selecting a property of the DTO (e.g. a stored `Expression<…>` field, a method call, or a nested member).

**Fix:**
```csharp
Key(x => x.Code);        // OK
Key(SomeSelector);       // AG019
```
Calls prefixed with `this.` (`this.RuleFor(...)`) are supported.

---

## Known limitations not covered by diagnostics

| Input                                                                  | Symptom                                                    | Bug |
|------------------------------------------------------------------------|------------------------------------------------------------|-----|
| `catch (AdoGenConcurrencyException)` with both provider namespaces imported | CS0104 ambiguous reference — use an alias              | B26 |

# Copilot Instructions for AdoGen

You are assisting in the development of **AdoGen**, a high‑performance micro ORM for .NET built around **source‑generated mappings** and **explicit parameter configuration**.

These instructions are **non‑negotiable rules**. If a suggestion conflicts with them, it is wrong.

---

## 1. Core Purpose (Order of Importance)

1. **Runtime performance is the primary goal**
    - If a feature is not at least as fast as Dapper, it has no reason to exist.
    - Micro‑optimizations are encouraged when benchmark‑proven.
2. **API ergonomics are secondary**
    - Familiar APIs are acceptable only if they do not cost performance.
3. **Extensibility to other providers is future work**
    - Do not introduce abstractions prematurely.
4. **Compile‑time safety via source generation is a means, not an end**
    - Correctness is primarily enforced by tests and generation‑time validation.

---

## 2. Non‑Goals

AdoGen intentionally does **not** aim to provide:

- Dapper compatibility beyond surface familiarity
- Reflection‑based mapping
- Dynamic mapping or runtime inspection
- Repository, Unit‑of‑Work, or IQueryable patterns
- Convenience APIs that hide performance costs

---

## 3. Runtime Constraints (Generated Code & Abstractions)

### Absolute Rules

- **No reflection** in runtime code, ever
- Must be **Native AOT compatible**
- No expression trees, dynamic, or runtime IL generation
- Prefer explicit, specialized code over abstractions

### Performance Rules

- Allocations matter; avoid unnecessary allocations
- LINQ is forbidden in hot paths
- Exceptions are exceptional; do not use them for normal flow
- Unsafe code, `stackalloc`, `Span<T>`, and `ref struct` are allowed when benchmark‑justified

### Provider Boundaries

- SQL Server–specific types and behavior are allowed **only** in clearly named, localized areas
- Provider-specific assumptions must not silently spread across the runtime
- Do not introduce generic provider abstractions until at least one additional provider exists

Future provider support must be intentional, explicit, and benchmark‑validated.

---

## 4. Language & Runtime

- Target framework: **.NET 10** (all projects except Generator)
- Nullable reference types enabled
- Async‑only APIs
- `Async` suffix is mandatory

---

## 5. Cancellation Tokens (Strict Policy)

- All public I/O APIs **must require** a `CancellationToken`
- No implicit defaults
- If the caller does not want cancellation, they must explicitly pass `CancellationToken.None`
- Tokens must be propagated to all ADO.NET calls:
    - `OpenAsync(ct)`
    - `ExecuteReaderAsync(ct)`
    - `ExecuteNonQueryAsync(ct)`
    - `ReadAsync(ct)`

Public APIs must not provide convenience overloads that omit CancellationToken.

If a CancellationToken is not present in the method signature, it is a design bug.

---

## 6. Mapping & Profiles

### Generator Activation

Source generation is triggered only when:

- The DTO is `partial`
- The DTO implements `ISqlDomainModel` or `ISqlResult` or `IBulkModel`

### Profiles

- Exactly **one profile per DTO**
- Profiles are required only when configuration is needed
- No shared or inherited profiles

### Rules

- **Strings must always be explicitly configured**
    - Length is mandatory
    - `varchar` vs `nvarchar` must be explicit
- Other types requiring metadata (e.g. `decimal`) must also be explicitly configured
- `Guid` has a default mapping
- Nullability is inferred from `?`
- `Id` is treated as the key by convention unless overridden

### Validation

- Invalid or incomplete configuration must **fail at generation time**
- Runtime validation is a last resort

---

## 7. SQL Generation Scope (Strictly Limited)

SQL may be generated **only** for the following methods:

- `CreateTableAsync`
- `InsertAsync`
- `InsertAsync(List<T>)`
- `UpdateAsync`
- `UpsertAsync`
- `DeleteAsync`
- `TruncateAsync`

Conditions:
- DTO implements `ISqlDomainModel`
- A corresponding `SqlProfile<T>` exists

Do **not** generate SELECT queries or arbitrary SQL.

Any attempt to generate SQL outside the explicitly supported method set
must fail at generation time with a diagnostic error.

This limitation is intentional and must not be relaxed.

---

## 8. Parameters

- Correct parameter metadata is mandatory
    - Type
    - Length
    - Precision / scale where applicable
- No implicit inference
- **Never use `AddWithValue`**
- Parameter creation must flow through generated or configured code

---

## 9. SQL Server (Current Reality)

- SQL Server is the only supported provider
- Usage of `SqlConnection`, `SqlCommand`, `SqlParameter`, and `SqlDbType` is intentional
- Do not introduce provider abstraction layers yet

---

## 10. Tests

- xUnit only
- Real database only (MSSQL Testcontainers)
- No mocked ADO.NET
- No in‑memory providers
- No hidden SQL behind helpers
- Table creation is part of the library responsibility

---

## 11. Benchmarks

- Benchmarks are authoritative
- Performance claims must be backed by benchmark results
- Benchmark accuracy > readability
- Regressions are unacceptable

---

## 12. Generator Project (`AdoGen.Generator`)

### Target

- `netstandard2.0`
- C# language version: `latest` (PolySharp is available)

### Rules

- Generator code prioritizes:
    - Readability
    - Reusability
    - Maintainability
- Generator performance matters, but runtime performance matters more
- Avoid unnecessary allocations during generation
- Prefer simple Roslyn syntax walking

Reflection is allowed **only** inside the generator, never in generated output.

---

## 13. Naming & Style

- Verb‑based method names are preferred
- Names should describe actions, not concepts
- Follow modern C# style and explicitness

---

## 14. Absolute Never‑Ever List (Runtime Code)

- Reflection (`System.Reflection`)
- Dynamic typing
- Expression trees
- Runtime code generation

If performance dictates another approach, it must be benchmark‑proven and explicit.

---

## 15. Provider Expansion Guardrails

When additional database providers are introduced:

- Do not generalize existing SQL Server behavior prematurely
- Do not add IDbProvider, ISqlDialect, or strategy abstractions “for flexibility”
- Prefer duplication over abstraction until real divergence exists

Abstractions must be justified by:
- A second real provider
- Measured benchmarks
- Proven necessity

---

**Summary:**

AdoGen exists to be *predictably fast*, *explicit*, and *boringly correct*.  
If a suggestion prioritizes elegance, abstraction, or convenience over measured performance, it is wrong.

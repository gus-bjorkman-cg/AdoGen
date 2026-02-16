# AdoGen.Generator

This project contains the **Roslyn source generator** that powers AdoGen’s mapping and SQL generation.

The generator exists to produce **explicit, allocation‑aware, reflection‑free runtime code**. It must never introduce behavior that would reduce runtime performance or Native AOT compatibility.

---

## Core Responsibilities

The generator is responsible for:

- Discovering eligible DTOs
    - `partial` types
    - Implementing `ISqlDomainModel` or `ISqlResult`
- Validating mapping configuration expressed via `SqlProfile<T>`
- Failing **at generation time** for invalid or incomplete configuration
- Emitting fully static runtime code with no reflection or dynamic behavior

The generator **does not** attempt to be clever at runtime. Any complexity belongs here, not in the generated output.

---

## Target & Language

- Target framework: `netstandard2.0`
- C# language version: `latest`
- PolySharp is available

This means:
- Modern C# syntax is allowed
- Runtime‑only APIs not available to `netstandard2.0` must not be used

---

## Design Priorities (In Order)

1. **Correctness of generated output**
    - Invalid configurations must fail fast during generation
2. **Maintainability**
    - Adding support for new SQL types or providers should be straightforward
3. **Readability**
    - Generator code is read far more often than it is executed
4. **Generator performance**
    - Slow generators degrade developer experience
    - Avoid unnecessary allocations and repeated semantic model queries

Runtime performance always outweighs generator performance.

---

## Generator Performance Guidelines

- Prefer syntax‑based analysis over semantic analysis where possible
- Cache semantic symbols aggressively
- Avoid LINQ in hot generator paths
- Do not allocate large temporary collections unnecessarily

The generator should feel invisible during normal development.

---

## Validation Rules

The generator must enforce:

- Exactly one `SqlProfile<T>` per configured DTO
- Mandatory configuration for:
    - `string` (length + varchar/nvarchar)
    - `decimal` (precision/scale)
    - Any type where provider metadata affects correctness or performance
- Clear, actionable diagnostics
    - Diagnostics should explain *what* is wrong and *how* to fix it

If something can be validated at generation time, it **must not** be deferred to runtime.

---

## Diagnostics Philosophy

Generator diagnostics are not merely correctness checks.

They exist to:
- Prevent performance regressions
- Enforce explicit configuration
- Make invalid states unrepresentable at runtime

If a misconfiguration could result in:
- Implicit parameter inference
- Suboptimal SQL types
- Runtime branching or reflection

Then it must fail during generation, not execution.

---

## Generated Code Rules (Non‑Negotiable)

Generated code must:

- Contain **no reflection**
- Be **Native AOT compatible**
- Avoid LINQ, dynamic, and expression trees
- Prefer explicit, specialized methods
- Propagate `CancellationToken` to all async ADO.NET calls

If a generated construct would be questionable to hand‑write in a hot path, it is wrong.

---

## Extensibility Notes

While SQL Server is the only supported provider today:

- Avoid baking provider‑specific assumptions into generator internals where unnecessary
- Keep SQL Server specifics localized so future providers can be added intentionally

Do **not** introduce abstractions until a second provider actually exists.
Provider seams should be named and localized, not abstracted.

---

## Summary

The generator is the *brain* of AdoGen.

It may be complex, but the code it emits must be:
- Predictable
- Explicit
- Fast

If a trade‑off exists, always choose **runtime performance and correctness** over generator convenience.

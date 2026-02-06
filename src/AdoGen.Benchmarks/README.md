# AdoGen.Benchmark

This project contains **authoritative performance benchmarks** for AdoGen.

Benchmarks exist to answer one question only:

> Is AdoGen at least as fast as the alternatives?

If the answer is ever "no", the change is wrong.

---

## Purpose

The benchmark suite is used to:

- Validate that AdoGen meets or exceeds Dapper performance
- Detect regressions early
- Justify low‑level or unsafe optimizations
- Compare trade‑offs against EF Core where relevant

Benchmarks are not examples. They are measurements.

---

## Benchmarks Are Contracts

Benchmark results define acceptable behavior.

Any change that:
- Improves readability
- Improves API elegance
- Reduces code duplication

But worsens benchmarks is considered a regression and must be reverted
unless explicitly justified and documented.

---

## Ground Rules

- **Benchmark accuracy > readability**
- Results matter more than aesthetics
- Micro‑benchmarks are allowed and encouraged
- Any performance‑motivated change must be benchmark‑defensible

If a benchmark is ugly but accurate, it is correct.

---

## Comparisons

Benchmarks may compare against:

- AdoGen
- Dapper
- EF Core

Comparisons must:

- Use equivalent SQL
- Use equivalent parameter types
- Avoid accidental advantages or disadvantages

Unfair benchmarks are worse than no benchmarks.

---

## Methodology

- Use real SQL Server instances via Testcontainers
- No in‑memory providers
- No mocked database layers
- Schema creation is part of the benchmark lifecycle

Benchmarks should reflect real execution paths.

---

## Allowed Techniques

The following are explicitly allowed when benchmark‑justified:

- `unsafe` code
- `stackalloc`
- `Span<T>` / `ReadOnlySpan<T>`
- Manual loops instead of LINQ

Readability may be sacrificed **only** in benchmarked hot paths.

---

## What Benchmarks Must Not Do

- Hide allocations behind helpers
- Measure logging, tracing, or diagnostics
- Mix unrelated scenarios in a single benchmark
- Rely on undefined behavior

Each benchmark should measure one thing clearly.

---

## Interpreting Results

- Focus on:
    - Allocations
    - Throughput
    - Latency
- Single runs are insufficient; trends matter
- Regressions must be explained or fixed

"It feels faster" is not a valid argument.

---

## Summary

Benchmarks are the **final authority** in AdoGen.

If a change improves elegance but worsens benchmarks, the benchmarks win.

Always optimize with evidence.

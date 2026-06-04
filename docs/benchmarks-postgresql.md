# Benchmarks — PostgreSQL

Full benchmark results for AdoGen vs Dapper and EF Core on PostgreSQL.

> **DapperNoType** = Dapper with untyped parameters and no `CancellationToken`.  
> **Dapper** = Dapper with typed parameters and `CancellationToken`.  
> **EfCoreComp** = EF Core with compiled queries.

---

## Environment

```
BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.100
[Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
```

---

## Query

| Type           | Method       |     Mean | Error     | StdDev     | Gen0   | Allocated |
|----------------|--------------|---------:|----------:|-----------:|------: |----------:|
| FirstOrDefault | AdoGen       | 189.3 µs |   3.63 µs |    4.19 µs |      - |     817 B |
| FirstOrDefault | DapperNoType | 203.2 µs |   1.95 µs |    1.63 µs |      - |   1.56 KB |
| FirstOrDefault | Dapper       | 204.0 µs |   3.21 µs |    2.84 µs |      - |   1.93 KB |
| FirstOrDefault | EfCore       | 217.5 µs |   4.08 µs |    8.70 µs | 1.0000 |  10.15 KB |
| FirstOrDefault | EfCoreComp   | 219.5 µs |   2.20 µs |    1.84 µs |      - |   3.48 KB |
| ToList         | AdoGen       | 19.11 µs |  0.381 µs |   0.356 µs | 0.0195 |     234 B |
| ToList         | DapperNoType | 20.21 µs |  0.320 µs |   0.284 µs | 0.0391 |     331 B |
| ToList         | Dapper       | 20.25 µs |  0.372 µs |   0.330 µs | 0.0391 |     381 B |
| ToList         | EfCoreComp   | 20.88 µs |  0.338 µs |   0.300 µs | 0.0391 |     410 B |
| ToList         | EfCore       | 21.30 µs |  0.323 µs |   0.269 µs | 0.1172 |    1198 B |

## Single-Row Operations

| Type   | Method       | Mean     | Error    | StdDev   | Allocated |
|--------|--------------|--------: |--------: |--------: |----------:|
| Insert | AdoGen       | 1.037 ms | 0.097 ms | 0.286 ms |   2.71 KB |
| Insert | DapperNoType | 1.050 ms | 0.116 ms | 0.340 ms |   2.77 KB |
| Insert | Dapper       | 1.109 ms | 0.079 ms | 0.230 ms |   3.52 KB |
| Insert | EfCore       | 2.561 ms | 0.193 ms | 0.562 ms |  15.63 KB |
| Update | DapperNoType | 0.940 ms | 0.146 ms | 0.425 ms |   2.77 KB |
| Update | AdoGen       | 0.989 ms | 0.099 ms | 0.291 ms |   2.70 KB |
| Update | Dapper       | 1.046 ms | 0.126 ms | 0.372 ms |   3.51 KB |
| Update | EfCore       | 2.405 ms | 0.183 ms | 0.533 ms |  15.89 KB |
| Delete | AdoGen       | 0.998 ms | 0.110 ms | 0.323 ms |   2.22 KB |
| Delete | Dapper       | 1.112 ms | 0.118 ms | 0.346 ms |   2.79 KB |
| Delete | DapperNoType | 1.127 ms | 0.085 ms | 0.250 ms |   2.31 KB |
| Delete | EfCore       | 2.096 ms | 0.188 ms | 0.550 ms |  14.65 KB |

## Multi-Row Insert (10 records)

| Type        | Method       | Mean     | Error    | StdDev   | Allocated |
|-------------|--------------|--------: |--------: |--------: |----------:|
| InsertMulti | AdoGen       | 1.297 ms | 0.119 ms | 0.349 ms |  15.55 KB |
| InsertMulti | AdoGenBulk   | 1.350 ms | 0.095 ms | 0.278 ms |  16.70 KB |
| InsertMulti | EfCore       | 2.695 ms | 0.259 ms | 0.762 ms |  69.34 KB |
| InsertMulti | DapperNoType | 3.107 ms | 0.078 ms | 0.223 ms |  18.70 KB |
| InsertMulti | Dapper       | 3.143 ms | 0.107 ms | 0.305 ms |  25.34 KB |

## Bulk Operations (1,000 records)

| Type       | Method     | Mean      | Error    | StdDev   | Allocated   |
|------------|------------|----------:|---------:|---------:|------------:|
| BulkDelete | AdoGen     |  2.206 ms | 0.185 ms | 0.534 ms |   18.21 KB  |
| BulkDelete | AdoGenBulk |  5.894 ms | 0.241 ms | 0.686 ms |    5.97 KB  |
| BulkDelete | EfCore     | 22.295 ms | 2.585 ms | 7.623 ms | 4188.48 KB  |
| BulkInsert | AdoGen     |  7.084 ms | 0.177 ms | 0.506 ms |    6.28 KB  |
| BulkInsert | EfCore     | 17.863 ms | 2.587 ms | 7.588 ms | 5808.94 KB  |
| BulkUpdate | AdoGen     |  7.732 ms | 0.155 ms | 0.433 ms |    5.69 KB  |
| BulkUpdate | EfCore     | 27.451 ms | 2.525 ms | 7.406 ms | 6000.23 KB  |

## Bulk Operations (10,000 records)

| Type          | Method | Mean       | Error    | StdDev   | Gen0      | Gen1      | Allocated    |
|---------------|--------|----------: |---------:|---------:|----------:|----------:|-------------:|
| BulkInsert10K | AdoGen |   46.01 ms | 0.914 ms | 1.423 ms |         - |         - |     9.82 KB  |
| BulkInsert10K | EfCore |  129.71 ms | 2.586 ms | 7.584 ms | 5000.0000 | 2000.0000 | 56409.96 KB  |

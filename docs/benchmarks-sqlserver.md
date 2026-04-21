# Benchmarks — SQL Server

Full benchmark results for AdoGen vs Dapper and EF Core on SQL Server.

> **DapperNT** = Dapper with untyped parameters and no `CancellationToken`.  
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

| Type           | Method     | Mean      | Error    | StdDev   | Gen0   | Allocated |
|----------------|------------|----------:|---------:|---------:|-------:|----------:|
| FirstOrDefault | AdoGen     |  389.4 µs | 15.36 µs | 45.29 µs |      - |   2.82 KB |
| FirstOrDefault | Dapper     |  397.3 µs | 13.74 µs | 40.50 µs |      - |   6.05 KB |
| FirstOrDefault | EfCoreComp |  402.7 µs | 13.57 µs | 40.01 µs |      - |    7.8 KB |
| FirstOrDefault | EfCore     |  418.2 µs | 15.40 µs | 45.40 µs |      - |  15.08 KB |
| FirstOrDefault | DapperNT   |  433.8 µs | 13.71 µs | 40.43 µs |      - |   5.89 KB |
| ToList         | AdoGen     |  38.80 µs |  0.771 µs | 0.825 µs |      - |     453 B |
| ToList         | EfCore     |  39.98 µs |  0.444 µs | 0.393 µs | 0.1563 |    1705 B |
| ToList         | DapperNT   |  39.99 µs |  0.691 µs | 0.768 µs | 0.0781 |     778 B |
| ToList         | EfCoreComp |  39.99 µs |  0.793 µs | 1.187 µs | 0.0781 |     835 B |
| ToList         | Dapper     |  40.12 µs |  0.787 µs | 1.024 µs | 0.0781 |     825 B |

## Single-Row Operations

| Type   | Method   | Mean     | Error    | StdDev   | Gen0 | Allocated |
|--------|----------|--------: |--------: |--------: |-----:|----------:|
| Insert | AdoGen   | 1.830 ms | 0.118 ms | 0.345 ms |    - |    5.3 KB |
| Insert | DapperNT | 1.902 ms | 0.109 ms | 0.317 ms |    - |   5.59 KB |
| Insert | Dapper   | 1.986 ms | 0.113 ms | 0.328 ms |    - |   6.48 KB |
| Insert | EfCore   | 2.642 ms | 0.191 ms | 0.558 ms |    - |  20.09 KB |
| Update | AdoGen   | 1.728 ms | 0.131 ms | 0.382 ms |    - |   5.17 KB |
| Update | Dapper   | 1.777 ms | 0.129 ms | 0.371 ms |    - |   6.32 KB |
| Update | DapperNT | 1.957 ms | 0.097 ms | 0.276 ms |    - |   5.52 KB |
| Update | EfCore   | 2.373 ms | 0.199 ms | 0.573 ms |    - | 142.53 KB |
| Delete | Dapper   | 1.835 ms | 0.178 ms | 0.513 ms |    - |   5.25 KB |
| Delete | AdoGen   | 1.837 ms | 0.154 ms | 0.448 ms |    - |   4.34 KB |
| Delete | DapperNT | 1.870 ms | 0.207 ms | 0.609 ms |    - |    4.8 KB |
| Delete | EfCore   | 2.411 ms | 0.227 ms | 0.669 ms |    - |  19.52 KB |

## Multi-Row Insert (10 records)

| Type       | Method     | Mean     | Error    | StdDev   | Gen0 | Allocated |
|------------|------------|--------: |--------: |--------: |-----:|----------:|
| InsertMulti | AdoGen     | 2.012 ms | 0.137 ms | 0.397 ms |    - |   21.2 KB |
| InsertMulti | AdoGenBulk | 2.030 ms | 0.125 ms | 0.358 ms |    - |  21.63 KB |
| InsertMulti | EfCore     | 2.964 ms | 0.243 ms | 0.708 ms |    - |  76.87 KB |
| InsertMulti | DapperNT   | 5.997 ms | 0.371 ms | 1.052 ms |    - |  35.44 KB |
| InsertMulti | Dapper     | 6.618 ms | 0.618 ms | 1.784 ms |    - |  43.69 KB |

## Bulk Operations (1,000 records)

| Type       | Method     | Mean      | Error    | StdDev   | Gen0 | Allocated   |
|------------|------------|----------:|---------:|---------:|-----:|------------:|
| BulkInsert | AdoGen     |  20.90 ms | 0.519 ms | 1.481 ms |    - |   161.98 KB |
| BulkInsert | EfCore     |  37.02 ms | 2.751 ms | 7.893 ms |    - |  6091.48 KB |
| BulkUpdate | AdoGen     |  22.34 ms | 0.759 ms | 2.154 ms |    - |    143.3 KB |
| BulkUpdate | EfCore     |  47.15 ms | 3.398 ms | 9.748 ms |    - |  7179.33 KB |
| BulkDelete | AdoGenBulk |  21.18 ms | 0.422 ms | 1.019 ms |    - |    131.4 KB |
| BulkDelete | EfCore     |  33.53 ms | 3.090 ms | 8.917 ms |    - |  4829.72 KB |

## Bulk Operations (10,000 records)

| Type          | Method | Mean      | Error    | StdDev    | Median    | Gen0      | Gen1      | Allocated    |
|---------------|--------|----------:|---------:|----------:|----------:|----------:|----------:|-------------:|
| BulkInsert10K | AdoGen |  83.97 ms | 1.648 ms |  3.292 ms |  82.85 ms |         - |         - |   1412.44 KB |
| BulkInsert10K | EfCore | 337.39 ms | 7.844 ms | 23.004 ms | 329.94 ms | 7000.0000 | 2000.0000 |  60923.84 KB |


# Benchmarks — SQL Server

Full benchmark results for AdoGen vs Dapper and EF Core on SQL Server.

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

| Type           | Method       |      Mean | Error     | StdDev     | Gen0   | Allocated |
|----------------|--------------|----------:|---------: |----------: |------: |----------:|
| FirstOrDefault | AdoGen       |  364.5 µs |   6.70 µs |    8.94 µs |      - |   2.75 KB |
| FirstOrDefault | Dapper       |  378.5 µs |   7.29 µs |   11.77 µs |      - |   6.47 KB |
| FirstOrDefault | EfCoreComp   |  382.6 µs |   7.46 µs |    8.59 µs |      - |   7.81 KB |
| FirstOrDefault | EfCore       |  397.5 µs |   7.88 µs |   11.56 µs | 1.0000 |  15.39 KB |
| FirstOrDefault | DapperNoType |  429.2 µs |   8.45 µs |   16.07 µs |      - |   5.91 KB |
| ToList         | AdoGen       |  36.26 µs |  0.718 µs |   1.314 µs | 0.0391 |     455 B |
| ToList         | EfCoreComp   |  37.72 µs |  0.752 µs |   1.502 µs | 0.0781 |     837 B |
| ToList         | EfCore       |  38.68 µs |  0.706 µs |   0.660 µs | 0.1563 |    1674 B |
| ToList         | Dapper       |  39.21 µs |  0.776 µs |   1.476 µs | 0.0781 |     824 B |
| ToList         | DapperNoType |  40.21 µs |  0.802 µs |   1.447 µs | 0.0781 |     778 B |

## Single-Row Operations

| Type   | Method       | Mean     | Error    | StdDev   | Allocated |
|--------|--------------|--------: |--------: |--------: |----------:|
| Insert | AdoGen       | 1.671 ms | 0.147 ms | 0.425 ms |   4.93 KB |
| Insert | DapperNoType | 1.833 ms | 0.162 ms | 0.469 ms |   5.60 KB |
| Insert | Dapper       | 1.859 ms | 0.165 ms | 0.481 ms |   6.46 KB |
| Insert | EfCore       | 2.184 ms | 0.201 ms | 0.588 ms |  20.09 KB |
| Update | Dapper       | 1.749 ms | 0.156 ms | 0.445 ms |   6.32 KB |
| Update | AdoGen       | 1.808 ms | 0.154 ms | 0.448 ms |   5.03 KB |
| Update | DapperNoType | 1.885 ms | 0.177 ms | 0.517 ms |   5.52 KB |
| Update | EfCore       | 2.206 ms | 0.264 ms | 0.774 ms |  20.66 KB |
| Delete | Dapper       | 1.629 ms | 0.178 ms | 0.515 ms |   5.25 KB |
| Delete | AdoGen       | 1.638 ms | 0.166 ms | 0.481 ms |   4.20 KB |
| Delete | DapperNoType | 1.797 ms | 0.151 ms | 0.441 ms |   4.80 KB |
| Delete | EfCore       | 2.213 ms | 0.236 ms | 0.694 ms |  19.48 KB |

## Multi-Row Insert (10 records)

| Type        | Method       | Mean     | Error    | StdDev   | Allocated |
|-------------|--------------|--------: |--------: |--------: |----------:|
| InsertMulti | AdoGen       | 1.756 ms | 0.180 ms | 0.527 ms |  20.99 KB |
| InsertMulti | AdoGenBulk   | 1.777 ms | 0.199 ms | 0.586 ms |  21.38 KB |
| InsertMulti | EfCore       | 2.402 ms | 0.243 ms | 0.717 ms |  76.87 KB |
| InsertMulti | DapperNoType | 5.087 ms | 0.113 ms | 0.327 ms |  35.14 KB |
| InsertMulti | Dapper       | 5.123 ms | 0.128 ms | 0.364 ms |  43.37 KB |

## Bulk Operations (1,000 records)

| Type       | Method     | Mean      | Error    | StdDev    | Allocated   |
|------------|------------|----------:|---------:|----------:|------------:|
| BulkInsert | AdoGen     |  17.48 ms | 0.347 ms |  0.927 ms |  161.09 KB  |
| BulkInsert | EfCore     |  37.84 ms | 3.645 ms | 10.691 ms | 6092.66 KB  |
| BulkUpdate | AdoGen     |  27.47 ms | 0.532 ms |  0.711 ms |  142.59 KB  |
| BulkUpdate | EfCore     |  44.46 ms | 1.044 ms |  2.839 ms | 6854.09 KB  |
| BulkDelete | AdoGenBulk |  21.41 ms | 0.426 ms |  1.129 ms |  131.43 KB  |
| BulkDelete | EfCore     |  27.95 ms | 0.646 ms |  1.680 ms | 4830.59 KB  |
| BulkDelete | AdoGen     |  33.28 ms | 0.657 ms |  0.920 ms |  460.44 KB  |

## Bulk Operations (10,000 records)

| Type          | Method | Mean       | Error    | StdDev   | Gen0      | Gen1      | Allocated    |
|---------------|--------|----------: |---------:|---------:|----------:|----------:|-------------:|
| BulkInsert10K | AdoGen |   80.03 ms | 1.419 ms | 2.167 ms |         - |         - |  1413.91 KB  |
| BulkInsert10K | EfCore |  316.29 ms | 6.054 ms | 5.663 ms | 7000.0000 | 2000.0000 | 60903.52 KB  |

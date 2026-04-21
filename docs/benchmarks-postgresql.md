# Benchmarks — PostgreSQL

Full benchmark results for AdoGen vs Dapper and EF Core on PostgreSQL.

> Benchmarks will be added here once the PostgreSQL benchmark suite is finalized.
>
> The methodology and environment are identical to the
> [SQL Server benchmarks](benchmarks-sqlserver.md).

| BenchType      | Method         |       Mean |     Error |    StdDev |   Gen0 |  Allocated |
|----------------|----------------|-----------:|----------:|----------:|-------:|-----------:|
| FirstOrDefault | AdoGen         |   212.8 us |   5.19 us |  15.21 us |      - |      817 B |
| FirstOrDefault | Dapper         |   246.3 us |  10.08 us |  29.09 us |      - |     1976 B |
| FirstOrDefault | DapperNoType   |   253.6 us |  15.19 us |  43.82 us |      - |     1592 B |
| FirstOrDefault | EfCore         |   261.2 us |  16.11 us |  47.24 us | 1.0000 |    10681 B |
| FirstOrDefault | EfCoreCompiled |   287.3 us |  15.42 us |  45.46 us |      - |     3560 B |
| ToList         | AdoGen         |   19.74 us |  0.321 us |  0.285 us |      - |      234 B |
| ToList         | DapperNoType   |   20.39 us |  0.401 us |  0.394 us | 0.0391 |      331 B |
| ToList         | EfCoreCompiled |   20.63 us |  0.200 us |  0.177 us | 0.0391 |      409 B |
| ToList         | Dapper         |   20.88 us |  0.400 us |  0.374 us | 0.0391 |      381 B |
| ToList         | EfCore         |   20.94 us |  0.336 us |  0.315 us | 0.1172 |     1197 B |
| Insert         | AdoGen         |   1.077 ms | 0.0908 ms | 0.2604 ms |      - |    2.71 KB |    
| Insert         | DapperNoType   |   1.165 ms | 0.0862 ms | 0.2488 ms |      - |    2.77 KB |   
| Insert         | Dapper         |   1.200 ms | 0.0853 ms | 0.2476 ms |      - |    3.52 KB |
| Insert         | EfCore         |   2.620 ms | 0.1738 ms | 0.5126 ms |      - |   15.63 KB |
| InsertMulti    | AdoGen         | 1,086.9 us | 110.77 us |  319.6 us |      - |   12.84 KB |
| InsertMulti    | AdoGenBulk     | 1,221.7 us | 103.72 us |  305.8 us |      - |   14.02 KB |
| InsertMulti    | EfCore         | 2,353.7 us | 148.12 us |  432.1 us |      - |   69.34 KB |   
| InsertMulti    | DapperNoType   | 3,003.8 us |  71.77 us |  207.1 us |      - |    18.7 KB |   
| InsertMulti    | Dapper         | 3,142.8 us |  93.52 us |  265.3 us |      - |   25.34 KB |
| Delete         | AdoGen         |   1.168 ms | 0.1185 ms | 0.3458 ms |      - |    2.22 KB |
| Delete         | DapperNoType   |   1.211 ms | 0.1098 ms | 0.3238 ms |      - |    2.31 KB |
| Delete         | Dapper         |   1.253 ms | 0.0695 ms | 0.2026 ms |      - |    2.79 KB |
| Delete         | EfCore         |   2.583 ms | 0.2285 ms | 0.6630 ms |      - |   14.65 KB |
| Update         | AdoGen         |   1.145 ms | 0.1151 ms | 0.3394 ms |      - |     2.7 KB |
| Update         | DapperNoType   |   1.311 ms | 0.1216 ms | 0.3526 ms |      - |    2.77 KB |
| Update         | Dapper         |   1.338 ms | 0.1174 ms | 0.3444 ms |      - |    3.51 KB |
| Update         | EfCore         |   2.373 ms | 0.2232 ms | 0.6511 ms |      - |   15.89 KB |
| BulkDelete     | AdoGen         |   2.772 ms | 0.1595 ms | 0.4679 ms |      - |  381.84 KB |
| BulkDelete     | AdoGenBulk     |   5.681 ms | 0.1113 ms | 0.1596 ms |      - |    6.59 KB |
| BulkDelete     | EfCore         |  22.564 ms | 2.6056 ms | 7.6418 ms |      - | 4188.48 KB |
| BulkUpdate     | AdoGen         |   7.653 ms | 0.1519 ms | 0.3668 ms |      - |    6.59 KB |
| BulkUpdate     | EfCore         |  27.018 ms | 2.2083 ms | 6.4068 ms |      - | 6000.05 KB |
| BulkInsert     | AdoGen         |   7.846 ms | 0.1561 ms | 0.3188 ms |      - | 1080.26 KB |
| BulkInsert     | EfCore         |  18.308 ms | 2.9644 ms | 8.7406 ms |      - |  5807.8 KB |

| BenchType       | Method | Mean      | Error    | StdDev   | Gen0      | Gen1      | Allocated   |
|---------------- |------- |----------:|---------:|---------:|----------:|----------:|------------:|
| PgBulkInsert10K | AdoGen |  32.58 ms | 0.593 ms | 1.290 ms |         - |         - |     9.18 KB |
| PgBulkInsert10K | EfCore | 125.23 ms | 2.486 ms | 2.863 ms | 5000.0000 | 2000.0000 | 56379.15 KB |
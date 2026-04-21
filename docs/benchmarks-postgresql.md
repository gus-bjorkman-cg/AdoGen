# Benchmarks — PostgreSQL

Full benchmark results for AdoGen vs Dapper and EF Core on PostgreSQL.

> Benchmarks will be added here once the PostgreSQL benchmark suite is finalized.
>
> The methodology and environment are identical to the
> [SQL Server benchmarks](benchmarks-sqlserver.md).

| BenchType      | Method         |       Mean |     Error |    StdDev |   Gen0 |  Allocated |
|----------------|----------------|-----------:|----------:|----------:|-------:|-----------:|
| FirstOrDefault | Dapper         |   203.7 us |   3.00 us |   2.80 us |      - |     1976 B |
| FirstOrDefault | DapperNoType   |   206.2 us |   3.33 us |   3.12 us |      - |     1592 B |
| FirstOrDefault | AdoGen         |   207.8 us |   4.09 us |   7.77 us |      - |      817 B |
| FirstOrDefault | EfCore         |   215.4 us |   4.18 us |   4.11 us | 1.0000 |    10681 B |
| FirstOrDefault | EfCoreCompiled |   223.6 us |   4.33 us |   4.25 us |      - |     3560 B |
| ToList         | AdoGen         |   19.74 us |  0.321 us |  0.285 us |      - |      234 B |
| ToList         | DapperNoType   |   20.39 us |  0.401 us |  0.394 us | 0.0391 |      331 B |
| ToList         | EfCoreCompiled |   20.63 us |  0.200 us |  0.177 us | 0.0391 |      409 B |
| ToList         | Dapper         |   20.88 us |  0.400 us |  0.374 us | 0.0391 |      381 B |
| ToList         | EfCore         |   20.94 us |  0.336 us |  0.315 us | 0.1172 |     1197 B |
| Insert         | AdoGen         |   963.0 us | 101.33 us |  297.2 us |      - |    2.85 KB |   
| InsertMulti    | AdoGen         | 1,086.9 us | 110.77 us |  319.6 us |      - |   12.98 KB |   
| Insert         | DapperNoType   | 1,114.3 us | 108.95 us |  319.5 us |      - |    2.47 KB |   
| Insert         | Dapper         | 1,167.3 us |  85.63 us |  242.9 us |      - |    3.52 KB |   
| InsertMulti    | AdoGenBulk     | 1,221.7 us | 103.72 us |  305.8 us |      - |   14.02 KB |   
| Insert         | EfCore         | 2,270.0 us | 232.94 us |  675.8 us |      - |   15.63 KB |   
| InsertMulti    | EfCore         | 2,353.7 us | 148.12 us |  432.1 us |      - |   69.34 KB |   
| InsertMulti    | DapperNoType   | 3,003.8 us |  71.77 us |  207.1 us |      - |    18.7 KB |   
| InsertMulti    | Dapper         | 3,142.8 us |  93.52 us |  265.3 us |      - |   25.34 KB |   
| Delete         | DapperNoType   |   977.8 us |  98.82 us |  289.8 us |      - |    2.31 KB |
| Delete         | Dapper         |   988.3 us |  88.70 us |  260.1 us |      - |    2.79 KB |
| Delete         | AdoGen         |   996.2 us | 108.49 us |  318.2 us |      - |    2.36 KB |
| Delete         | EfCore         | 2,124.5 us | 240.75 us |  706.1 us |      - |   14.65 KB |
| Update         | Dapper         |   1.115 ms | 0.1174 ms | 0.3444 ms |      - |    3.51 KB |
| Update         | AdoGen         |   1.137 ms | 0.1151 ms | 0.3394 ms |      - |    2.84 KB |
| Update         | DapperNoType   |   1.183 ms | 0.1216 ms | 0.3526 ms |      - |    2.77 KB |
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
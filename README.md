# AdoGen

**A high‑performance, reflection‑free micro‑ORM for .NET**  
built around source‑generated mappings and explicit parameter metadata.

AdoGen focuses on **predictable performance**, **Native AOT compatibility**,  
and **doing parameter binding correctly**—without magic, reflection, or runtime code generation.

AdoGen outperforms others in multi insert & bulk operations.
---

Project description
-------------------
Alfa release, only supporting .net 10 & Sql server.
Configuration inspired by [`FluentValidation`](https://github.com/JeremySkinner/FluentValidation).
Querying inspired by [`Dapper`](https://github.com/DapperLib/Dapper)

Implementing ISqlResult triggers source generation for mapper helper classes.
Implementing ISqlDomainModel triggers source generation of standard crud implementations.
Creating a SqlProfile triggers db parameter generation.

Class or record must be partial to trigger source generation.
SqlProfile must be imlpemented for ISqlDomainModel to work.
ISqlDomainModel inherits from ISqlResult, so it can be used for query results as well.

Benchmarks
-------------------
DapperNoType (NT) = dapper with parameter as object and no cancellation token.
Dapper = dapper with typed parameters and cancellation token.
InsertMulti is benchmarked on insert of 10 records.

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores                                                                                                                                  
.NET SDK 10.0.100                                                                                                                                                                  
[Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a                                                                                                         
DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
```
| Type           | Method     | Mean       | Error     | StdDev    | Gen0   | Allocated  |
|--------------- |----------- |-----------:|----------:|----------:|-------:|-----------:|
| FirstOrDefault | AdoGen     |   389.4 us |  15.36 us |  45.29 us |      - |    2.82 KB |
| FirstOrDefault | Dapper     |   397.3 us |  13.74 us |  40.50 us |      - |    6.05 KB |
| FirstOrDefault | EfCoreComp |   402.7 us |  13.57 us |  40.01 us |      - |     7.8 KB |
| FirstOrDefault | EfCore     |   418.2 us |  15.40 us |  45.40 us |      - |   15.08 KB |
| FirstOrDefault | DapperNT   |   433.8 us |  13.71 us |  40.43 us |      - |    5.89 KB |
| ToList         | AdoGen     |   38.80 us |  0.771 us |  0.825 us |      - |      453 B |
| ToList         | EfCore     |   39.98 us |  0.444 us |  0.393 us | 0.1563 |     1705 B |
| ToList         | DapperNT   |   39.99 us |  0.691 us |  0.768 us | 0.0781 |      778 B |
| ToList         | EfCoreComp |   39.99 us |  0.793 us |  1.187 us | 0.0781 |      835 B |
| ToList         | Dapper     |   40.12 us |  0.787 us |  1.024 us | 0.0781 |      825 B |
| Delete         | Dapper     |   1.835 ms | 0.1778 ms | 0.5129 ms |      - |    5.25 KB |
| Delete         | AdoGen     |   1.837 ms | 0.1543 ms | 0.4477 ms |      - |    4.34 KB |
| Delete         | DapperNT   |   1.870 ms | 0.2065 ms | 0.6088 ms |      - |     4.8 KB |
| Delete         | EfCore     |   2.411 ms | 0.2269 ms | 0.6692 ms |      - |   19.52 KB |
| Update         | AdoGen     |   1.728 ms | 0.1309 ms | 0.3819 ms |      - |    5.17 KB |
| Update         | Dapper     |   1.777 ms | 0.1286 ms | 0.3712 ms |      - |    6.32 KB |
| Update         | DapperNT   |   1.957 ms | 0.0966 ms | 0.2755 ms |      - |    5.52 KB |
| Update         | EfCore     |   2.373 ms | 0.1987 ms | 0.5734 ms |      - |  142.53 KB |
| Insert         | AdoGen     |   1.830 ms | 0.1178 ms | 0.3454 ms |      - |     5.3 KB |
| Insert         | DapperNT   |   1.902 ms | 0.1093 ms | 0.3170 ms |      - |    5.59 KB |
| Insert         | Dapper     |   1.986 ms | 0.1130 ms | 0.3279 ms |      - |    6.48 KB |
| Insert         | EfCore     |   2.642 ms | 0.1911 ms | 0.5575 ms |      - |   20.09 KB |
| InsertMulti 10 | AdoGen     |   2.012 ms | 0.1370 ms | 0.3974 ms |      - |    21.2 KB |
| InsertMulti 10 | AdoGenBulk |   2.030 ms | 0.1248 ms | 0.3580 ms |      - |   21.63 KB |
| InsertMulti 10 | EfCore     |   2.964 ms | 0.2426 ms | 0.7076 ms |      - |   76.87 KB |
| InsertMulti 10 | DapperNT   |   5.997 ms | 0.3707 ms | 1.0516 ms |      - |   35.44 KB |
| InsertMulti 10 | Dapper     |   6.618 ms | 0.6183 ms | 1.7839 ms |      - |   43.69 KB |
| BulkInsert  1K | AdoGen     |   20.90 ms |  0.519 ms |  1.481 ms |      - |  161.98 KB |
| BulkInsert  1K | EfCore     |   37.02 ms |  2.751 ms |  7.893 ms |      - | 6091.48 KB |
| BulkUpdate  1K | AdoGen     |   22.34 ms |  0.759 ms |  2.154 ms |      - |   143.3 KB |
| BulkUpdate  1K | EfCore     |   47.15 ms |  3.398 ms |  9.748 ms |      - | 7179.33 KB |
| BulkDelete  1K | AdoGenBulk |  21.183 ms | 0.4218 ms | 1.0186 ms |      - |   131.4 KB |
| BulkDelete  1K | EfCore     |  33.528 ms | 3.0904 ms | 8.9165 ms |      - | 4829.72 KB |
| BulkDelete  1K | AdoGen     |  74.423 ms | 1.4864 ms | 2.0346 ms |      - |   459.7 KB |


| Type          | Method | Mean      | Error    | StdDev    | Median    | Gen0      | Gen1      | Allocated   |
|-------------- |------- |----------:|---------:|----------:|----------:|----------:|----------:|------------:|
| BulkInsert10K | AdoGen |  83.97 ms | 1.648 ms |  3.292 ms |  82.85 ms |         - |         - |  1412.44 KB |
| BulkInsert10K | EfCore | 337.39 ms | 7.844 ms | 23.004 ms | 329.94 ms | 7000.0000 | 2000.0000 | 60923.84 KB |
```

Example usage
-------------------

```csharp
public sealed partial record User(Guid Id, string Name, string Email) : ISqlResult;

public sealed class UserProfile : SqlProfile<User>
{
    public UserProfile()
    {
        RuleFor(x => x.Name).VarChar(20);
        RuleFor(x => x.Email).VarChar(50);
    }
}

public sealed partial record Order(Guid Id, string ProductName, Guid UserId) : ISqlDomainModel;

public sealed class OrderProfile : SqlProfile<Order>
{
    public OrderProfile()
    {
        RuleFor(x => x.ProductName).VarChar(50);
    }
}

public sealed class Sample
{
    public async ValueTask UserMethods(CancellationToken ct)
    {
        await connection.QueryAsync<User>("SELECT * FROM Users", ct);
        await connection.QueryFirstOrDefaultAsync<User>("SELECT TOP(1) * FROM Users WHERE Name = @Name", 
            UserSql.CreateParameterName("John Doe"), ct);
    }
    
    public async ValueTask OrderMethods(Order order, CancellationToken ct)
    {
        await connection.QueryAsync<Order>("SELECT * FROM Orders", ct);
        await connection.QueryFirstOrDefaultAsync<Order>("SELECT TOP(1) * FROM Orders WHERE ProductName = @ProductName", 
            OrderSql.CreateParameterName("Car"), ct);
        
        await connection.InsertAsync(order, ct);
        await connection.UpdateAsync(order, ct);
        await connection.UpsertAsync(order, ct);
        await connection.DeleteAsync(order, ct);
    }
}

/* OUTPUT:
UserSql.g.cs
UserMapper.g.cs

OrderSql.g.cs
OrderMapper.g.cs
OrderDomainOps.g.cs
*/
```
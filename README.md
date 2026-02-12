# AdoGen

**A high‑performance, reflection‑free micro‑ORM for .NET**  
built around source‑generated mappings and explicit parameter metadata.

AdoGen focuses on **predictable performance**, **Native AOT compatibility**,  
and **doing parameter binding correctly**—without magic, reflection, or runtime code generation.

AdoGen outperforms others in multi insert & bulk operations.
For single operations, dapper and AdoGen are too close to measure mean. 
Either can win mean for a given run, but AdoGen always win in allocations. 
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
DapperNoType = dapper with parameter as object and no cancellation token.
Dapper = dapper with typed parameters and cancellation token.
InsertMulti is benchmarked on insert of 10 records.

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores                                                                                                                                  
.NET SDK 10.0.100                                                                                                                                                                  
[Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a                                                                                                         
DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
```
| Type           | Method         | Mean       | Error     | StdDev    | Allocated  |
|--------------- |--------------- |-----------:|----------:|----------:|-----------:|
| FirstOrDefault | AdoGen         |   389.4 us |  15.36 us |  45.29 us |    2.82 KB |
| FirstOrDefault | Dapper         |   397.3 us |  13.74 us |  40.50 us |    6.05 KB |
| FirstOrDefault | EfCoreCompiled |   402.7 us |  13.57 us |  40.01 us |     7.8 KB |
| FirstOrDefault | EfCore         |   418.2 us |  15.40 us |  45.40 us |   15.08 KB |
| FirstOrDefault | DapperNoType   |   433.8 us |  13.71 us |  40.43 us |    5.89 KB |
| ToList         | AdoGen         |   2.562 ms | 0.2021 ms | 0.5863 ms |    6.97 KB |
| ToList         | DapperNoType   |   2.635 ms | 0.1950 ms | 0.5720 ms |    8.01 KB |
| ToList         | Dapper         |   2.724 ms | 0.1595 ms | 0.4653 ms |    8.46 KB |
| ToList         | EfCoreCompiled |   2.958 ms | 0.1609 ms | 0.4694 ms |  132.08 KB |
| ToList         | EfCore         |   3.070 ms | 0.1620 ms | 0.4675 ms |  141.14 KB |
| Delete         | AdoGen         |   2.258 ms | 0.2010 ms | 0.5864 ms |    4.32 KB |
| Delete         | DapperNoType   |   2.319 ms | 0.1966 ms | 0.5765 ms |     4.8 KB |
| Delete         | Dapper         |   2.334 ms | 0.2294 ms | 0.6763 ms |    5.41 KB |
| Delete         | EfCore         |   3.473 ms | 0.2400 ms | 0.7000 ms |  140.98 KB |
| Update         | AdoGen         |   2.973 ms | 0.1844 ms | 0.5350 ms |    5.16 KB |
| Update         | DapperNoType   |   3.271 ms | 0.1894 ms | 0.5525 ms |    5.52 KB |
| Update         | Dapper         |   3.335 ms | 0.1846 ms | 0.5355 ms |    6.48 KB |
| Update         | EfCore         |   4.343 ms | 0.2109 ms | 0.6152 ms |  142.12 KB |
| Insert         | Dapper         |   3.204 ms | 0.1998 ms | 0.5861 ms |    6.63 KB |
| Insert         | DapperNoType   |   3.394 ms | 0.2149 ms | 0.6301 ms |    5.59 KB |
| Insert         | AdoGen         |   3.437 ms | 0.2221 ms | 0.6548 ms |    5.34 KB |
| Insert         | EfCore         |   3.881 ms | 0.3081 ms | 0.8890 ms |  141.57 KB |
| InsertMulti 10 | AdoGen         |   2.123 ms | 0.1919 ms | 0.5627 ms |    21.2 KB |
| InsertMulti 10 | AdoGenBulk     |   2.210 ms | 0.1240 ms | 0.3537 ms |   21.41 KB |
| InsertMulti 10 | EfCore         |   3.454 ms | 0.2932 ms | 0.8506 ms |   76.71 KB |
| InsertMulti 10 | Dapper         |   7.907 ms | 0.9379 ms | 2.6453 ms |   43.56 KB |
| InsertMulti 10 | DapperNoType   |   7.991 ms | 0.9167 ms | 2.6153 ms |   35.42 KB |
| BulkInsert  1K | AdoGen         |   24.58 ms |  0.918 ms |  2.618 ms |  162.32 KB |
| BulkInsert  1K | EfCore         |   47.65 ms |  5.246 ms | 15.302 ms | 6414.42 KB |
| BulkUpdate  1K | AdoGen         |   22.34 ms |  0.759 ms |  2.154 ms |   143.3 KB |
| BulkUpdate  1K | EfCore         |   47.15 ms |  3.398 ms |  9.748 ms | 7179.33 KB |
| BulkDelete  1K | AdoGenBulk     |   27.34 ms |  0.881 ms |  2.455 ms |  131.46 KB |
| BulkDelete  1K | EfCore         |   34.73 ms |  3.116 ms |  8.890 ms | 4829.72 KB |
| BulkDelete  1K | AdoGen         |   78.22 ms |  1.218 ms |  0.951 ms |   459.7 KB |


| BenchType     | Method | Mean     | Error    | StdDev   | Median   | Gen0      | Gen1      | Allocated |
|-------------- |------- |---------:|---------:|---------:|---------:|----------:|----------:|----------:|
| BulkInsert10K | AdoGen | 120.0 ms |  2.47 ms |  7.01 ms | 117.5 ms |         - |         - |   1.38 MB |
| BulkInsert10K | EfCore | 425.2 ms | 14.83 ms | 43.74 ms | 421.9 ms | 7000.0000 | 1000.0000 |  61.41 MB |
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
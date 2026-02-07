# AdoGen

**A high‑performance, reflection‑free micro‑ORM for .NET**  
built around source‑generated mappings and explicit parameter metadata.

AdoGen focuses on **predictable performance**, **Native AOT compatibility**,  
and **doing parameter binding correctly**—without magic, reflection, or runtime code generation.

AdoGen outperforms others in multi insert.
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
| Type           | Method         | Mean      | Error     | StdDev    | Allocated |
|--------------- |--------------- |----------:|----------:|----------:|----------:|
| FirstOrDefault | Dapper         |  1.993 ms | 0.2068 ms | 0.6066 ms |    6.9 KB |
| FirstOrDefault | AdoGen         |  2.282 ms | 0.1691 ms | 0.4934 ms |   5.77 KB |
| FirstOrDefault | DapperNoType   |  2.514 ms | 0.1980 ms | 0.5808 ms |   6.51 KB |
| FirstOrDefault | EfCore         |  3.104 ms | 0.1528 ms | 0.4433 ms | 140.38 KB |
| FirstOrDefault | EfCoreCompiled |  3.309 ms | 0.1463 ms | 0.4245 ms | 132.11 KB |
| ToList         | AdoGen         |  2.562 ms | 0.2021 ms | 0.5863 ms |   6.97 KB |
| ToList         | DapperNoType   |  2.635 ms | 0.1950 ms | 0.5720 ms |   8.01 KB |
| ToList         | Dapper         |  2.724 ms | 0.1595 ms | 0.4653 ms |   8.46 KB |
| ToList         | EfCoreCompiled |  2.958 ms | 0.1609 ms | 0.4694 ms | 132.08 KB |
| ToList         | EfCore         |  3.070 ms | 0.1620 ms | 0.4675 ms | 141.14 KB |
| Delete         | AdoGen         |  2.258 ms | 0.2010 ms | 0.5864 ms |   4.32 KB |
| Delete         | DapperNoType   |  2.319 ms | 0.1966 ms | 0.5765 ms |    4.8 KB |
| Delete         | Dapper         |  2.334 ms | 0.2294 ms | 0.6763 ms |   5.41 KB |
| Delete         | EfCore         |  3.473 ms | 0.2400 ms | 0.7000 ms | 140.98 KB |
| Update         | AdoGen         |  2.973 ms | 0.1844 ms | 0.5350 ms |   5.16 KB |
| Update         | DapperNoType   |  3.271 ms | 0.1894 ms | 0.5525 ms |   5.52 KB |
| Update         | Dapper         |  3.335 ms | 0.1846 ms | 0.5355 ms |   6.48 KB |
| Update         | EfCore         |  4.343 ms | 0.2109 ms | 0.6152 ms | 142.12 KB |
| Insert         | Dapper         |  3.204 ms | 0.1998 ms | 0.5861 ms |   6.63 KB |
| Insert         | DapperNoType   |  3.394 ms | 0.2149 ms | 0.6301 ms |   5.59 KB |
| Insert         | AdoGen         |  3.437 ms | 0.2221 ms | 0.6548 ms |   5.34 KB |
| Insert         | EfCore         |  3.881 ms | 0.3081 ms | 0.8890 ms | 141.57 KB |
| InsertMulti    | AdoGen         |  3.693 ms | 0.2012 ms | 0.5900 ms |  20.43 KB |
| InsertMulti    | EfCore         |  4.667 ms | 0.2000 ms | 0.5865 ms | 199.68 KB |
| InsertMulti    | Dapper         |  9.974 ms | 0.7979 ms | 2.1843 ms |  44.02 KB |
| InsertMulti    | DapperNoType   | 10.025 ms | 0.7496 ms | 2.0896 ms |  35.42 KB |
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
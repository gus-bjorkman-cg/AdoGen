Ado code gen for C#, reflection free and AOT compatible db querying.
======================

Project description
-------------------
Alfa release, only supporting .net 10.
Configuration inspired by [`FluentValidation`](https://github.com/JeremySkinner/FluentValidation).
Querying inspired by [`Dapper`](https://github.com/DapperLib/Dapper)

Implement ISqlResult on a partial class or record and the source generator will implement Sql and mapper help 
class. Or ISqlDomainModel if you also want some standard crud implementations.

Benchmarks
-------------------
DapperNoType = dapper with parameter as object and no cancellation token.
Dapper = dapper with typed parameters and cancellation token.

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores                                                                                                                                  
.NET SDK 10.0.100                                                                                                                                                                  
[Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a                                                                                                         
DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
```
| BenchType    | Method         | Mean       | Error     | StdDev    | Median     | Gen0    | Gen1   | Allocated |
|------------- |--------------- |-----------:|----------:|----------:|-----------:|--------:|-------:|----------:|
| AdoGen       | FirstOrDefault |   445.9 us |  15.14 us |  44.64 us |   465.3 us |       - |      - |   6.29 KB |
| Dapper       | FirstOrDefault |   448.0 us |  14.96 us |  44.12 us |   454.1 us |       - |      - |   7.06 KB |
| DapperNoType | FirstOrDefault |   494.6 us |  15.27 us |  45.01 us |   511.7 us |       - |      - |   6.55 KB |
| EfCompiled   | FirstOrDefault |   512.7 us |  15.23 us |  44.92 us |   530.2 us |  9.7656 | 0.9766 |  82.75 KB |
| EfCore       | FirstOrDefault |   537.4 us |  14.27 us |  42.08 us |   551.8 us | 10.7422 | 0.9766 |  90.33 KB |
| AdoGen       | ToList         |   468.1 us |   9.25 us |  21.79 us |   476.4 us |  0.4883 |      - |   7.62 KB |
| DapperNoType | ToList         |   490.7 us |  14.29 us |  42.12 us |   499.1 us |  0.9766 |      - |   8.13 KB |
| Dapper       | ToList         |   493.7 us |  13.97 us |  41.19 us |   508.9 us |  0.9766 |      - |   8.58 KB |
| EfCompiled   | ToList         |   495.9 us |  14.67 us |  43.26 us |   516.2 us |  9.7656 | 0.9766 |  82.79 KB |
| EfCore       | ToList         |   514.0 us |  13.86 us |  40.88 us |   525.2 us | 10.7422 | 0.9766 |  91.22 KB |
| DapperNoType | Insert         |   683.8 us |  16.51 us |  48.69 us |   699.4 us |       - |      - |   6.26 KB |
| EfCore       | Insert         |   803.9 us |  36.31 us | 107.08 us |   758.7 us |  9.7656 |      - |  92.52 KB |
| AdoGen       | Insert         |   879.3 us |  60.15 us | 177.36 us |   865.8 us |       - |      - |   6.71 KB |
| AdoGen       | InsertMulti    |   912.9 us |  42.81 us | 125.56 us |   927.5 us |  1.9531 |      - |  28.36 KB |
| EfCore       | InsertMulti    | 1,107.8 us |  35.74 us | 105.38 us | 1,160.0 us | 17.5781 | 1.9531 | 155.59 KB |
| DapperNoType | InsertMulti    | 6,679.3 us | 304.40 us | 897.53 us | 6,647.7 us |       - |      - |  43.12 KB |
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
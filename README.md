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
The benchmarks compare generated code from AdoExtensions against Dapper for querying lists and single items.
However, mean execution time differes between runs so far..

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores                                                                                                                                  
.NET SDK 10.0.100                                                                                                                                                                  
[Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a                                                                                                         
DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
```
| Method            | Mean     | Error    | StdDev   | Median   | Gen0   | Allocated |
|------------------ |---------:|---------:|---------:|---------:|-------:|----------:|
| AdoExtensions     | 393.3 us | 10.97 us | 31.99 us | 376.8 us |      - |   5.59 KB |                                                                                             
| Dapper            | 395.2 us |  9.92 us | 28.94 us | 381.0 us |      - |   6.88 KB |
| AdoExtensionsList | 413.5 us |  8.17 us | 17.42 us | 406.0 us | 1.9531 |  17.43 KB |
| DapperList        | 413.7 us | 10.07 us | 28.91 us | 400.5 us | 1.9531 |  21.39 KB |
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
        
        await connection.Insert(order, ct);
        await connection.Update(order, ct);
        await connection.Upsert(order, ct);
        await connection.Delete(order, ct);
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
Ado code gen for C#, reflection free and AOT compatible db querying.
======================

Project description
-------------------
Alfa release, only supporting .net 10.
Configuration inspired by [`FluentValidation`](https://github.com/JeremySkinner/FluentValidation).
Querying inspired by [`Dapper`](https://github.com/DapperLib/Dapper)

Implement ISqlResult on a partial class or record and the source generator will implement Sql and mapper help 
class. Or ISqlDomainModel if you also want some standard crud implementations.

```csharp
public sealed partial record User(Guid Id, string Name, string Email) : ISqlResult;

public sealed class UserProfile : SqlProfile<User>
{
    public UserProfile()
    {
        Configure(x => x.Name).VarChar(20);
        Configure(x => x.Email).VarChar(50);
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
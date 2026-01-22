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
public sealed partial record User(Guid Id, string Name, string Email) : ISqlDomainModel;

public sealed class UserProfile : SqlProfile<User>
{
    public UserProfile()
    {
        Configure(x => x.Name).VarChar(20);
        Configure(x => x.Email).VarChar(50);
    }
}

public sealed class Sample
{
    public async ValueTask Methods()
    {
        await connection.QueryAsync<User>("SELECT * FROM Users", cancellationToken);
        await connection.QueryFirstOrDefaultAsync<User>("SELECT TOP(1) * FROM Users WHERE Name = @Name", 
            UserSql.CreateParameterName("John Doe"), cancellationToken);
    }
}

/* OUTPUT:
UserSql.g.cs
UserMapper.g.cs
UserDomainOps.g.cs
*/
```
namespace AdoGen.Sample.Features.Users;

public sealed record InsertUserCommand(string Name, string Email); 

public sealed class InsertUserCommandHandler(string connectionString)
{
    public async ValueTask<User> Handle(InsertUserCommand command, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        var user = new User(Guid.CreateVersion7(), command.Name, command.Email);
        await connection.Insert(user, ct);
        
        return user;
    }
}
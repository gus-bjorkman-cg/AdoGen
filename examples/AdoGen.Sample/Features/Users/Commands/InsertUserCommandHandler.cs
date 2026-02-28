namespace AdoGen.Sample.Features.Users.Commands;

public sealed record InsertUserCommand(string Name, string Email); 

public sealed class InsertUserCommandHandler(string connectionString)
{
    public async ValueTask<User> SqlServer(InsertUserCommand command, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        var user = new User(Guid.CreateVersion7(), command.Name, command.Email);
        await connection.InsertAsync(user, ct);
        
        return user;
    }
    
    public async ValueTask<User> NpgSql(InsertUserCommand command, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);

        var user = new User(Guid.CreateVersion7(), command.Name, command.Email);
        await connection.InsertAsync(user, ct);

        return user;
    }
}
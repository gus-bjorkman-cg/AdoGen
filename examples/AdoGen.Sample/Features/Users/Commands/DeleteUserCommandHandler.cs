namespace AdoGen.Sample.Features.Users.Commands;

public sealed record DeleteUserCommand(Guid Id);

public sealed class DeleteUserCommandHandler(string connectionString)
{
    public async ValueTask SqlServer(DeleteUserCommand command, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.DeleteAsync(new User(command.Id, "", ""), ct);
    }
    
    public async ValueTask NpgSql(DeleteUserCommand command, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.DeleteAsync(new User(command.Id, "", ""), ct);
    }
}
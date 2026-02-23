namespace AdoGen.Sample.Features.Users.Commands;

public sealed record DeleteUserCommand(Guid Id);

public sealed class DeleteUserCommandHandler(string connectionString)
{
    public async ValueTask Handle(DeleteUserCommand command, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.DeleteAsync(new User(command.Id, "", ""), ct);
    }
}
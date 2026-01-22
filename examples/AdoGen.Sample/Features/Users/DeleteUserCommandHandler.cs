namespace AdoGen.Sample.Features.Users;

public sealed record DeleteUserCommand(Guid Id);

public sealed class DeleteUserCommandHandler(string connectionString)
{
    public async ValueTask Handle(DeleteUserCommand command, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.Delete(new User(command.Id, "", ""), ct);
    }
}
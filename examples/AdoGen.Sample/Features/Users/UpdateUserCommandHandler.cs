namespace AdoGen.Sample.Features.Users;

public sealed record UpdateUserCommand(User User);

public sealed class UpdateUserCommandHandler(string connectionString)
{
    public async ValueTask Handle(UpdateUserCommand command, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.Update(command.User, ct);
    }
}
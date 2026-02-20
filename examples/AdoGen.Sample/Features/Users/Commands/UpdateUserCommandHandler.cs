namespace AdoGen.Sample.Features.Users.Commands;

public sealed record UpdateUserCommand(User User);

public sealed class UpdateUserCommandHandler(string connectionString)
{
    public async ValueTask Handle(UpdateUserCommand command, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.UpdateAsync(command.User, ct);
    }
}
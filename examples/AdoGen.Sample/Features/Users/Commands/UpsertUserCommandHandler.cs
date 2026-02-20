namespace AdoGen.Sample.Features.Users.Commands;

public sealed record UpsertUserCommand(User User);
public sealed class UpsertUserCommandHandler(string connectionString)
{
    public async ValueTask Handle(UpsertUserCommand command, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.UpsertAsync(command.User, ct);
    }
}
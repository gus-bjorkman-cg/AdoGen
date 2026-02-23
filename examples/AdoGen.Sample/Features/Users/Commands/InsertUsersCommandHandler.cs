namespace AdoGen.Sample.Features.Users.Commands;

public sealed record InsertUsersCommand(List<User> Users);
public sealed class InsertUsersCommandHandler(string connectionString)
{
    public async ValueTask Handle(InsertUsersCommand command, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.InsertAsync(command.Users, ct);
    }
}
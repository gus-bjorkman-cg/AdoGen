namespace AdoGen.Sample.Features.Users.Commands;

public record struct TruncateUsersCommand
{
    public static TruncateUsersCommand Instance { get; } = new();
}

public sealed class TruncateUsersCommandHandler(string connectionString)
{
    public async ValueTask Handle(TruncateUsersCommand command, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.TruncateAsync<User>(ct);
    }
}
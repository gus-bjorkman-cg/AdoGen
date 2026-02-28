namespace AdoGen.Sample.Features.Users.Commands;

public record struct TruncateUsersCommand
{
    public static TruncateUsersCommand Instance { get; } = new();
}

public sealed class TruncateUsersCommandHandler(string connectionString)
{
    public async ValueTask SqlServer(TruncateUsersCommand command, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.TruncateAsync<User>(ct);
    }
    
    public async ValueTask NpgSql(TruncateUsersCommand command, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.TruncateAsync<User>(ct);
    }
}
namespace AdoGen.Sample.Features.Users.Commands;

public sealed record UpsertUserCommand(User User);
public sealed class UpsertUserCommandHandler(string connectionString)
{
    public async ValueTask SqlServer(UpsertUserCommand command, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.UpsertAsync(command.User, ct);
    }
    
    public async ValueTask NpgSql(UpsertUserCommand command, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.UpsertAsync(command.User, ct);
    }
}
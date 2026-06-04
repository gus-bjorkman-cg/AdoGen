namespace AdoGen.Sample.Features.Users.Commands;

public sealed record InsertUsersCommand(List<User> Users);
public sealed class InsertUsersCommandHandler(string connectionString)
{
    public async ValueTask SqlServer(InsertUsersCommand command, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.InsertAsync(command.Users, ct);
    }
    
    public async ValueTask NpgSql(InsertUsersCommand command, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.InsertAsync(command.Users, ct);
    }
}
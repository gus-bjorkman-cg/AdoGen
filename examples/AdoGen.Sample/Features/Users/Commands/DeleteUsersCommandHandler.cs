namespace AdoGen.Sample.Features.Users.Commands;

public sealed record DeleteUsersCommand(List<Guid> Ids);
public sealed class DeleteUsersCommandHandler(string connectionString)
{
    public async ValueTask SqlServer(DeleteUsersCommand command, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.DeleteAsync<User, Guid>(command.Ids, ct);
    }
    
    public async ValueTask NpgSql(DeleteUsersCommand command, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.DeleteAsync<User, Guid>(command.Ids, ct);
    }
}
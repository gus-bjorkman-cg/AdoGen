namespace AdoGen.Sample.Features.Users.Commands;

public sealed record PatchUserSqlCommand(UserPatch Patch);
public sealed record PatchUserNpgsqlCommand(UserPatch Patch);

public sealed class PatchUserCommandHandler(string connectionString)
{
    public async ValueTask<int> SqlServer(PatchUserSqlCommand command, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        return await connection.PatchAsync(command.Patch, ct);
    }

    public async ValueTask<int> NpgSql(PatchUserNpgsqlCommand command, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        return await connection.PatchAsync(command.Patch, ct);
    }
}

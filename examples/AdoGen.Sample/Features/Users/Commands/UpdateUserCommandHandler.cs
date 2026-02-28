namespace AdoGen.Sample.Features.Users.Commands;

public sealed record UpdateUserCommand(User User);

public sealed class UpdateUserCommandHandler(string connectionString)
{
    public async ValueTask SqlServer(UpdateUserCommand command, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.UpdateAsync(command.User, ct);
    }
    
    public async ValueTask NpgSql(UpdateUserCommand command, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.UpdateAsync(command.User, ct);
    }
}
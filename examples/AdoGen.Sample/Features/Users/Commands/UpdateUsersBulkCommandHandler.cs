namespace AdoGen.Sample.Features.Users.Commands;

public sealed record UpdateUsersBulkCommand(List<User> Users);
public sealed class UpdateUsersBulkCommandHandler(string connectionString)
{
    public async ValueTask Handle(UpdateUsersBulkCommand command, CancellationToken ct)
    {
        var userBulk = new UserBulk(command.Users.Count);
        userBulk.UpdateRange(command.Users);
        
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = connection.BeginTransaction();
        await userBulk.SaveChangesAsync(connection, transaction, ct);
        await transaction.CommitAsync(ct);
    }
}
namespace AdoGen.Sample.Features.Users.Commands;

public sealed record InsertUsersBulkCommand(List<User> Users);
public sealed class InsertUsersBulkCommandHandler(string connectionString)
{
    public async ValueTask Handle(InsertUsersBulkCommand command, CancellationToken ct)
    {
        var userBulk = new UserBulk(command.Users.Count);
        userBulk.AddRange(command.Users);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct);
        await userBulk.SaveChangesAsync(connection, transaction, ct);
        await transaction.CommitAsync(ct);
    }
}
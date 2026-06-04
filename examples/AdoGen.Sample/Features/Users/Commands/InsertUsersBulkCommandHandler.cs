namespace AdoGen.Sample.Features.Users.Commands;

public sealed record InsertUsersBulkCommand(List<User> Users);
public sealed class InsertUsersBulkCommandHandler(string connectionString)
{
    public async ValueTask SqlServer(InsertUsersBulkCommand command, CancellationToken ct)
    {
        var userBulk = new UserBulk(command.Users.Count);
        userBulk.AddRange(command.Users);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct);
        await userBulk.SaveChangesAsync(connection, transaction, ct);
        await transaction.CommitAsync(ct);
    }
    
    public async ValueTask NpgSql(InsertUsersBulkCommand command, CancellationToken ct)
    {
        var userBulk = new UserNpgsqlBulk(command.Users.Count);
        userBulk.AddRange(command.Users);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await userBulk.SaveChangesAsync(connection, transaction, ct);
        await transaction.CommitAsync(ct);
    }
}
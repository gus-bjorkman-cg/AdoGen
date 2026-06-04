namespace AdoGen.Sample.Features.Users.Commands;

public sealed record DeleteUsersBulkCommand(List<User> Users);
public sealed class DeleteUsersBulkCommandHandler(string connectionString)
{
    public async ValueTask SqlServer(DeleteUsersBulkCommand command, CancellationToken ct)
    {
        var bulk = new UserBulk(command.Users.Count);
        bulk.RemoveRange(command.Users);
        
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct);
        await bulk.SaveChangesAsync(connection, transaction, ct);
        await transaction.CommitAsync(ct);
    }
    
    public async ValueTask NpgSql(DeleteUsersBulkCommand command, CancellationToken ct)
    {
        var bulk = new UserNpgsqlBulk(command.Users.Count);
        bulk.RemoveRange(command.Users);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await bulk.SaveChangesAsync(connection, transaction, ct);
        await transaction.CommitAsync(ct);
    }
}
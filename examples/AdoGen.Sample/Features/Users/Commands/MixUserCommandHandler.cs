namespace AdoGen.Sample.Features.Users.Commands;

public sealed record MixUserCommand(List<User> UserToInsert, List<User> UserToUpdate, List<User> UserToDelete);
public sealed class MixUserCommandHandler(string connectionString)
{
    public async ValueTask SqlServer(MixUserCommand command, CancellationToken ct)
    {
        var userBulk = new UserBulk();
        userBulk.AddRange(command.UserToInsert);
        userBulk.UpdateRange(command.UserToUpdate);
        userBulk.RemoveRange(command.UserToDelete);
        
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct);
        await userBulk.SaveChangesAsync(connection, transaction, ct);
        await transaction.CommitAsync(ct);
    }
    
    public async ValueTask NpgSql(MixUserCommand command, CancellationToken ct)
    {
        var userBulk = new UserNpgsqlBulk();
        userBulk.AddRange(command.UserToInsert);
        userBulk.UpdateRange(command.UserToUpdate);
        userBulk.RemoveRange(command.UserToDelete);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await userBulk.SaveChangesAsync(connection, transaction, ct);
        await transaction.CommitAsync(ct);
    }
}
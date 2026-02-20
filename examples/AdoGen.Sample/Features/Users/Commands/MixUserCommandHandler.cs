namespace AdoGen.Sample.Features.Users.Commands;

public sealed record MixUserCommand(List<User> UserToInsert, List<User> UserToUpdate, List<User> UserToDelete);
public sealed class MixUserCommandHandler(string connectionString)
{
    public async ValueTask Handle(MixUserCommand command, CancellationToken ct)
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
}
namespace AdoGen.Sample.Features.Users;

public sealed record GetUserByEmailQuery(string Email);

public sealed class GetUserByEmailQueryHandler(string connectionString)
{
    private const string Sql = "SELECT * FROM Users WHERE Email = @Email";
    
    public async ValueTask<User?> Handle(GetUserByEmailQuery query, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        return await connection.QueryFirstOrDefaultAsync<User>(Sql, UserSql.CreateParameterEmail(query.Email), ct);
    }
}
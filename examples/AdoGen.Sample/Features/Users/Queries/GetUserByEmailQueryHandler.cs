namespace AdoGen.Sample.Features.Users.Queries;

public sealed record GetUserByEmailQuery(string Email);

public sealed class GetUserByEmailQueryHandler(string connectionString)
{
    public async ValueTask<User?> SqlServer(GetUserByEmailQuery query, CancellationToken ct)
    {
        const string sql = "SELECT * FROM Users WHERE Email = @Email";
        await using var connection = new SqlConnection(connectionString);
        return await connection.QueryFirstOrDefaultAsync<User>(sql, UserSql.CreateParameterEmail(query.Email), ct);
    }

    public async ValueTask<User?> NpgSql(GetUserByEmailQuery query, CancellationToken ct)
    {
        const string sql = """SELECT * FROM "public"."Users" WHERE "Email" = @Email""";
        await using var connection = new NpgsqlConnection(connectionString);
        return await connection.QueryFirstOrDefaultAsync<User>(sql, UserNpgsql.CreateParameterEmail(query.Email), ct);
    }
}
namespace AdoGen.Sample.Features.Users.Queries;

public sealed record GetUsersQuery
{
    private GetUsersQuery() {}
    public static GetUsersQuery Instance { get; } = new();
}

public sealed class GetUsersQueryHandler(string connectionString)
{
    public async ValueTask<List<User>> SqlServer(GetUsersQuery query, CancellationToken ct)
    {
        const string sql = "SELECT * FROM Users";
        await using var connection = new SqlConnection(connectionString);
        return await connection.QueryAsync<User>(sql, ct);
    }
    
    public async ValueTask<List<User>> NpgSql(GetUsersQuery query, CancellationToken ct)
    {
        const string sql = """SELECT * FROM "public"."Users" """;
        await using var connection = new NpgsqlConnection(connectionString);
        return await connection.QueryAsync<User>(sql, ct);
    }
}
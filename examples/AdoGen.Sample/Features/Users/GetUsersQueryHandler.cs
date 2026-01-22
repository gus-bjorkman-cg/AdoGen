namespace AdoGen.Sample.Features.Users;

public sealed record GetUsersQuery
{
    private GetUsersQuery() {}
    public static GetUsersQuery Instance { get; } = new();
}

public sealed class GetUsersQueryHandler(string connectionString)
{
    private const string Sql = "SELECT * FROM Users";

    public async ValueTask<List<User>> Handle(GetUsersQuery query, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        return await connection.QueryAsync<User>(Sql, ct);
    }
}
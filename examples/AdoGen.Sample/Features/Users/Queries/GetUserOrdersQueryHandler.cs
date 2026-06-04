using AdoGen.Sample.Features.Orders;

namespace AdoGen.Sample.Features.Users.Queries;

public sealed record GetUserOrdersResponse(User User, List<Order> Orders);
public sealed record GetUserOrdersQuery
{
    private GetUserOrdersQuery(){}
    public static GetUserOrdersQuery Instance { get; } = new();
}

public sealed class GetUserOrdersQueryHandler(string connectionString)
{
    public async ValueTask<List<GetUserOrdersResponse>> SqlServer(GetUserOrdersQuery request, CancellationToken ct)
    {
        const string sql =
            """
            SELECT * FROM USERS;
            SELECT * FROM ORDERS 
            """;
        
        await using var connection = new SqlConnection(connectionString);
        var reader = await connection.QueryMultiAsync(sql, ct);

        var users = await reader.QueryAsync<User>(ct);
        var orders = await reader.QueryAsync<Order>(ct);

        var response = new List<GetUserOrdersResponse>();
        
        foreach (var user in users)
        {
            var userOrders = orders.Where(x => x.UserId == user.Id).ToList();
            response.Add(new GetUserOrdersResponse(user, userOrders));
        }
        
        return response;
    }
    
    public async ValueTask<List<GetUserOrdersResponse>> NpgSql(GetUserOrdersQuery request, CancellationToken ct)
    {
        const string sql =
            """
            SELECT * FROM "public"."Users";
            SELECT * FROM "public"."Orders"
            """;
        
        await using var connection = new NpgsqlConnection(connectionString);
        var reader = await connection.QueryMultiAsync(sql, ct);

        var users = await reader.QueryAsync<User>(ct);
        var orders = await reader.QueryAsync<Order>(ct);

        var response = new List<GetUserOrdersResponse>();

        foreach (var user in users)
        {
            var userOrders = orders.Where(x => x.UserId == user.Id).ToList();
            response.Add(new GetUserOrdersResponse(user, userOrders));
        }

        return response;
    }
}
using AdoGen.Sample.Features.Orders;

namespace AdoGen.Sample.Features.Users;

public sealed record GetUserOrdersResponse(User User, List<Order> Orders);
public sealed record GetUserOrdersQuery
{
    private GetUserOrdersQuery(){}
    public static GetUserOrdersQuery Instance { get; } = new();
}

public sealed class GetUserOrdersQueryHandler(string connectionString)
{
    private const string Sql =
        """
        SELECT * FROM USERS;
        SELECT * FROM ORDERS 
        """;
    
    public async ValueTask<List<GetUserOrdersResponse>> Handle(GetUserOrdersQuery request, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        var reader = await connection.QueryMultiAsync(Sql, ct);

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
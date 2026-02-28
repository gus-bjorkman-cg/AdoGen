namespace AdoGen.Sample.Features.Orders;

public sealed partial record Order(Guid Id, string ProductName, Guid UserId) : ISqlDomainModel, INpgsqlDomainModel;

public sealed class OrderProfile : SqlProfile<Order>
{
    public OrderProfile()
    {
        RuleFor(x => x.ProductName).VarChar(50);
    }
}

public sealed class OrderNpgsqlProfile : NpgsqlProfile<Order>
{
    public OrderNpgsqlProfile()
    {
        RuleFor(x => x.ProductName).Varchar(50);
    }
}
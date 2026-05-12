namespace AdoGen.Sample.Features.Orders;

public sealed partial record Order(Guid Id, string ProductName, Guid UserId, int Version) : ISqlDomainModel, INpgsqlDomainModel;
public sealed partial record Order2(Guid Id, string ProductName, Guid Version) : ISqlDomainModel, INpgsqlDomainModel;

public sealed class OrderProfile : SqlProfile<Order>
{
    public OrderProfile()
    {
        RuleFor(x => x.ProductName).VarChar(50);
        RuleFor(x => x.Version).ConcurrencyToken();
    }
}

public sealed class OrderNpgsqlProfile : NpgsqlProfile<Order>
{
    public OrderNpgsqlProfile()
    {
        RuleFor(x => x.ProductName).VarChar(50);
        RuleFor(x => x.Version).ConcurrencyToken();
    }
}

public sealed class OrderProfile2 : SqlProfile<Order2>
{
    public OrderProfile2()
    {
        RuleFor(x => x.ProductName).VarChar(50);
        RuleFor(x => x.Version).ConcurrencyToken();
    }
}

public sealed class OrderNpgsqlProfile2 : NpgsqlProfile<Order2>
{
    public OrderNpgsqlProfile2()
    {
        RuleFor(x => x.ProductName).VarChar(50);
        RuleFor(x => x.Version).ConcurrencyToken();
    }
}
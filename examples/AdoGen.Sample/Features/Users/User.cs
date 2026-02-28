namespace AdoGen.Sample.Features.Users;

public sealed partial record User(Guid Id, string Name, string Email) : ISqlBulkModel, INpgsqlBulkModel;

public sealed class UserSqlProfile : SqlProfile<User>
{
    public UserSqlProfile()
    {
        RuleFor(x => x.Name).VarChar(20);
        RuleFor(x => x.Email).VarChar(50);
    }
}
public sealed class UserNpgsqlProfile : NpgsqlProfile<User>
{
    public UserNpgsqlProfile()
    {
        RuleFor(x => x.Name).Varchar(20);
        RuleFor(x => x.Email).Varchar(50);
    }
}
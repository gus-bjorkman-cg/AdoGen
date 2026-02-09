namespace AdoGen.Sample.Features.Users;

public sealed partial record User(Guid Id, string Name, string Email) : ISqlBulkModel;

public sealed class UserProfile : SqlProfile<User>
{
    public UserProfile()
    {
        RuleFor(x => x.Name).VarChar(20);
        RuleFor(x => x.Email).VarChar(50);
    }
}
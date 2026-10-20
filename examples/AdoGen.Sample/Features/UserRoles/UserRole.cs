namespace AdoGen.Sample.Features.UserRoles;

// Key-only model (every column is part of the composite key) — exercises UPDATE/UPSERT with no writable columns.
public sealed partial record UserRole(Guid UserId, Guid RoleId) : ISqlDomainModel, INpgsqlDomainModel;

public sealed class UserRoleProfile : SqlProfile<UserRole>
{
    public UserRoleProfile()
    {
        Key(x => x.UserId);
        Key(x => x.RoleId);
    }
}

public sealed class UserRoleNpgsqlProfile : NpgsqlProfile<UserRole>
{
    public UserRoleNpgsqlProfile()
    {
        Key(x => x.UserId);
        Key(x => x.RoleId);
    }
}

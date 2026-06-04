namespace AdoGen.Generator.Models;

internal readonly record struct DbTypeRef(SqlProviderKind Provider, string EnumMember)
{
    public static DbTypeRef SqlServer(string sqlDbTypeMember) => new(SqlProviderKind.SqlServer, sqlDbTypeMember);
    public static DbTypeRef PostgreSql(string npgsqlDbTypeMember) => new(SqlProviderKind.PostgreSql, npgsqlDbTypeMember);

    public override string ToString() => EnumMember;
}
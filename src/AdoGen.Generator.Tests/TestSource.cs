namespace AdoGen.Generator.Tests;

internal static class TestSource
{
    public static string Source(ProviderKind provider, FileKind fileKind) =>
        $$"""
        using System;
        using AdoGen.{{provider.Namespace()}};
        
        namespace AdoGen.Generator.Tests;
        
        public sealed partial record User(Guid Id, string Name, string Email) : {{provider.InterfaceName(fileKind)}};
        
        public sealed class UserProfile : {{provider.ProfileName()}}<User>
        {
            public UserProfile()
            {
                RuleFor(x => x.Name).VarChar(20);
                RuleFor(x => x.Email).VarChar(50);
            }
        }
        """;
    
    private static string ProfileName(this  ProviderKind provider) =>
        provider switch
        {
            ProviderKind.SqlServer => "SqlProfile",
            ProviderKind.PostgreSql => "NpgsqlProfile",
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };
    
    private static string Namespace(this ProviderKind provider) =>
        provider switch
        {
            ProviderKind.SqlServer => "SqlServer",
            ProviderKind.PostgreSql => "PostgreSql",
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };
    
    private static string InterfaceName(this ProviderKind provider, FileKind fileKind) =>
        fileKind switch
        {
            FileKind.Parameters => provider switch
            {
                ProviderKind.SqlServer => "ISqlResult",
                ProviderKind.PostgreSql => "INpgsqlResult",
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
            },
            FileKind.Mapper => provider switch
            {
                ProviderKind.SqlServer => "ISqlResult",
                ProviderKind.PostgreSql => "INpgsqlResult",
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
            },
            FileKind.Domain => provider switch
            {
                ProviderKind.SqlServer => "ISqlDomainModel",
                ProviderKind.PostgreSql => "INpgsqlDomainModel",
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
            },
            FileKind.Bulk => provider switch
            {
                ProviderKind.SqlServer => "ISqlBulkModel",
                ProviderKind.PostgreSql => "INpgsqlBulkModel",
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(fileKind), fileKind, null)
        };
}
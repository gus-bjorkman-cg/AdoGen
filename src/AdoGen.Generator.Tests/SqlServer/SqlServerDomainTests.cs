namespace AdoGen.Generator.Tests.SqlServer;

public class SqlServerDomainTests
{
    private const string DtoName = "User";

    private const ProviderKind Provider = ProviderKind.SqlServer;
    private static readonly string ParametersFileName = DtoName.GetFileName(FileKind.Parameters, ProviderKind.SqlServer);
    private static readonly string MapperFileName = DtoName.GetFileName(FileKind.Mapper, ProviderKind.SqlServer);
    private static readonly string DomainFileName = DtoName.GetFileName(FileKind.Domain, ProviderKind.SqlServer);
    private static readonly string BulkFileName = DtoName.GetFileName(FileKind.Bulk, ProviderKind.SqlServer);

    [Fact]
    public void Diagnostics_ShouldBeEmpty_WhenValid() => 
        Source.RunGenerator(Provider).Diagnostics.Should().BeEmpty();

    [Fact]
    public void ParametersFile_ShouldBeGenerated() => 
        Source.RunGenerator(Provider).Result.GetGeneratedText(ParametersFileName).Should().NotBeEmpty();

    [Fact]
    public void MapperFile_ShouldBeGenerated() =>
        Source.RunGenerator(Provider).Result.GetGeneratedText(MapperFileName).Should().NotBeEmpty();
    
    [Fact]
    public Task DomainFile_ShouldMatchSnapshot() =>
        Verify(Source.RunGenerator(Provider).Result.GetGeneratedText(DomainFileName));
    
    [Fact]
    public void BulkFile_ShouldNotBeGenerated() =>
        Source.RunGenerator(Provider).Result.GetGeneratedText(BulkFileName).Should().BeEmpty();
    
    private const string Source =
        """
        using System;
        using AdoGen.SqlServer;

        namespace AdoGen.Generator.Tests;

        public sealed partial record User(Guid Id, string Name, string Email) : ISqlDomainModel;

        public sealed class UserProfile : SqlProfile<User>
        {
            public UserProfile()
            {
                RuleFor(x => x.Name).VarChar(20);
                RuleFor(x => x.Email).VarChar(50);
            }
        }
        """;
}
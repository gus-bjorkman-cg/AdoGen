namespace AdoGen.Generator.Tests;

public sealed class DomainTests
{
    public static TheoryData<AdoGenType, TestTypes> Cases => new()
    {
        { AdoGenType.SqlDomainModel, TestTypes.User },
        { AdoGenType.SqlDomainModel, TestTypes.AuditEvent },
        { AdoGenType.SqlDomainModel, TestTypes.TestType },
        { AdoGenType.SqlDomainModel, TestTypes.VersionedOrder },
        { AdoGenType.SqlDomainModel, TestTypes.VersionedOrderGuid },

        { AdoGenType.NpgsqlDomainModel, TestTypes.User },
        { AdoGenType.NpgsqlDomainModel, TestTypes.AuditEvent },
        { AdoGenType.NpgsqlDomainModel, TestTypes.TestType },
        { AdoGenType.NpgsqlDomainModel, TestTypes.VersionedOrder },
        { AdoGenType.NpgsqlDomainModel, TestTypes.VersionedOrderGuid },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Diagnostics_ShouldBeEmpty(AdoGenType genType, TestTypes testType) =>
        genType.RunUserGenerator(testType).Diagnostics.Should().BeEmpty();

    [Theory]
    [MemberData(nameof(Cases))]
    public void MapperFile_ShouldBeGenerated(AdoGenType genType, TestTypes testType) =>
        genType.GenerateFile(testType, genType.GetMapperType()).Should().NotBeEmpty();

    [Theory]
    [MemberData(nameof(Cases))]
    public Task DomainFile_ShouldMatchSnapshot(AdoGenType genType, TestTypes testType) =>
        Verify(genType.GenerateFile(testType, genType.GetDomainType()))
            .UseTextForParameters($"{testType.Name}.{genType.Provider.Name}");

    [Theory]
    [MemberData(nameof(Cases))]
    public void BulkFile_ShouldBeEmpty(AdoGenType genType, TestTypes testType) =>
        genType.GenerateFile(testType, genType.GetBulkType()).Should().BeEmpty();

    [Fact]
    public void AG012_ShouldEmitted_WhenInvalidConcurrencyTokenType()
    {
        // Arrange
        var source = """
                     using AdoGen.SqlServer;
                     namespace Test;
                     public sealed partial record Bar(Guid Id, string Token) : ISqlDomainModel;
                     public sealed class BarProfile : SqlProfile<Bar>
                     {
                         public BarProfile()
                         {
                             RuleFor(x => x.Token).VarChar(50).ConcurrencyToken();
                         }
                     }
                     """;
        
        // Act
        var result = source.RunGenerator(AdoGenType.SqlDomainModel);
        
        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG012");
    }
    
    [Fact]
    public void AG013_ShouldBeEmitted_WhenMultipleConcurrencyTokens()
    {
        // Arrange
        var source = """
            using AdoGen.SqlServer;
            namespace Test;
            public sealed partial record Foo(Guid Id, int Version, int Revision) : ISqlDomainModel;
            public sealed class FooProfile : SqlProfile<Foo>
            {
                public FooProfile()
                {
                    RuleFor(x => x.Version).ConcurrencyToken();
                    RuleFor(x => x.Revision).ConcurrencyToken();
                }
            }
            """;
        
        // Act
        var result = source.RunGenerator(AdoGenType.SqlDomainModel);
        
        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG013");
    }
}
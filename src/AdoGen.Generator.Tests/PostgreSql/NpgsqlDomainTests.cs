namespace AdoGen.Generator.Tests.PostgreSql;

public class NpgsqlDomainTests
{
    private static readonly AdoGenType GenType = AdoGenType.NpgsqlDomainModel;

    [Fact]
    public void Diagnostics_ShouldBeEmpty_WhenValid() => 
        GenType.RunUserGenerator.Diagnostics.Should().BeEmpty();

    [Fact]
    public void MapperFile_ShouldBeGenerated() =>
        GenType.GenerateUserFile(AdoGenType.NpgsqlMapper).Should().NotBeEmpty();
    
    [Fact]
    public Task DomainFile_ShouldMatchSnapshot() => Verify(GenType.GenerateUserFile(GenType));
    
    [Fact]
    public void BulkFile_ShouldNotBeGenerated() =>
        GenType.GenerateUserFile(AdoGenType.NpgsqlBulkModel).Should().BeEmpty();
}
namespace AdoGen.Generator.Tests.PostgreSql;

public class NpgsqlMapperTests
{
    private static readonly AdoGenType GenType = AdoGenType.NpgsqlMapper;
    
    [Fact]
    public void Diagnostics_ShouldBeEmpty_WhenValid() => 
        GenType.RunUserGenerator.Diagnostics.Should().BeEmpty();

    [Fact]
    public Task MapperFile_ShouldBeGenerated() => Verify(GenType.GenerateUserFile(GenType));
    
    [Fact]
    public void DomainFile_ShouldNotBeGenerated() =>
        GenType.GenerateUserFile(AdoGenType.NpgsqlDomainModel).Should().BeEmpty();
    
    [Fact]
    public void BulkFile_ShouldNotBeGenerated() =>
        GenType.GenerateUserFile(AdoGenType.NpgsqlBulkModel).Should().BeEmpty();
}
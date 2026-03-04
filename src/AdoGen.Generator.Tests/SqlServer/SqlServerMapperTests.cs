namespace AdoGen.Generator.Tests.SqlServer;

public class SqlServerMapperTests
{
    private static readonly AdoGenType GenType = AdoGenType.SqlMapper;
    
    [Fact]
    public void Diagnostics_ShouldBeEmpty_WhenValid() => 
        GenType.RunUserGenerator.Diagnostics.Should().BeEmpty();

    [Fact]
    public Task MapperFile_ShouldBeGenerated() => Verify(GenType.GenerateUserFile(GenType));
    
    [Fact]
    public void DomainFile_ShouldNotBeGenerated() =>
        GenType.GenerateUserFile(AdoGenType.SqlDomainModel).Should().BeEmpty();
    
    [Fact]
    public void BulkFile_ShouldNotBeGenerated() =>
        GenType.GenerateUserFile(AdoGenType.SqlBulkModel).Should().BeEmpty();
}
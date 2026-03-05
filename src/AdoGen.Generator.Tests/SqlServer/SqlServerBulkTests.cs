namespace AdoGen.Generator.Tests.SqlServer;

public class SqlServerBulkTests
{
    private static readonly AdoGenType GenType = AdoGenType.SqlBulkModel;
    
    [Fact]
    public void Diagnostics_ShouldBeEmpty_WhenValid() => 
        GenType.RunUserGenerator.Diagnostics.Should().BeEmpty();

    [Fact]
    public void MapperFile_ShouldBeGenerated() =>
        GenType.GenerateUserFile(AdoGenType.SqlMapper).Should().NotBeEmpty();
    
    [Fact]
    public void DomainFile_ShouldBeGenerated() => 
        GenType.GenerateUserFile(AdoGenType.SqlDomainModel).Should().NotBeEmpty();

    [Fact]
    public Task BulkFile_ShouldMatchSnapshot() => Verify(GenType.GenerateUserFile(GenType));
}
namespace AdoGen.Generator.Tests.PostgreSql;

public class NpgsqlBulkTests
{
    private static readonly AdoGenType GenType = AdoGenType.NpgsqlBulkModel;
    
    [Fact]
    public void Diagnostics_ShouldBeEmpty_WhenValid() => 
        GenType.RunUserGenerator.Diagnostics.Should().BeEmpty();

    [Fact]
    public void MapperFile_ShouldBeGenerated() =>
        GenType.GenerateUserFile(AdoGenType.NpgsqlMapper).Should().NotBeEmpty();
    
    [Fact]
    public void DomainFile_ShouldBeGenerated()
    {
        GenType.GenerateUserFile(AdoGenType.NpgsqlDomainModel).Should().NotBeEmpty();
    }

    [Fact]
    public Task BulkFile_ShouldMatchSnapshot() => Verify(GenType.GenerateUserFile(GenType));
}
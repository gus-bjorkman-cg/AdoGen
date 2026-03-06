// namespace AdoGen.Generator.Tests.SqlServer;
//
// public class SqlServerBulkTests
// {
//     private static readonly AdoGenType GenType = AdoGenType.SqlBulkModel;
//     private static readonly TestTypes TestType = TestTypes.User;
//     
//     [Fact]
//     public void Diagnostics_ShouldBeEmpty_WhenValid() => 
//         GenType.RunUserGenerator(TestType).Diagnostics.Should().BeEmpty();
//
//     [Fact]
//     public void MapperFile_ShouldBeGenerated() =>
//         GenType.GenerateFile(TestType, AdoGenType.SqlMapper).Should().NotBeEmpty();
//     
//     [Fact]
//     public void DomainFile_ShouldBeGenerated() => 
//         GenType.GenerateFile(TestType, AdoGenType.SqlDomainModel).Should().NotBeEmpty();
//
//     [Fact]
//     public Task BulkFile_ShouldMatchSnapshot() => Verify(GenType.GenerateFile(TestType, GenType));
// }
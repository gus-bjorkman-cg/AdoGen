// namespace AdoGen.Generator.Tests.SqlServer;
//
// public class SqlServerDomainTests
// {
//     private static readonly AdoGenType GenType = AdoGenType.SqlDomainModel;
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
//     public Task DomainFile_ShouldMatchSnapshot() => Verify(GenType.GenerateFile(TestType, GenType));
//     
//     [Fact]
//     public void BulkFile_ShouldNotBeGenerated() =>
//         GenType.GenerateFile(TestType, AdoGenType.SqlBulkModel).Should().BeEmpty();
// }
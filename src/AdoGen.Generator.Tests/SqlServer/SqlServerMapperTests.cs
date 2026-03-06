// namespace AdoGen.Generator.Tests.SqlServer;
//
// public class SqlServerMapperTests
// {
//     private static readonly AdoGenType GenType = AdoGenType.SqlMapper;
//     private static readonly TestTypes TestType = TestTypes.User;
//     
//     [Fact]
//     public void Diagnostics_ShouldBeEmpty_WhenValid() => 
//         GenType.RunUserGenerator(TestType).Diagnostics.Should().BeEmpty();
//
//     [Fact]
//     public Task MapperFile_ShouldBeGenerated() => Verify(GenType.GenerateFile(TestType, GenType));
//     
//     [Fact]
//     public void DomainFile_ShouldNotBeGenerated() =>
//         GenType.GenerateFile(TestType, AdoGenType.SqlDomainModel).Should().BeEmpty();
//     
//     [Fact]
//     public void BulkFile_ShouldNotBeGenerated() =>
//         GenType.GenerateFile(TestType, AdoGenType.SqlBulkModel).Should().BeEmpty();
// }
// namespace AdoGen.Generator.Tests.PostgreSql;
//
// public class NpgsqlBulkTests
// {
//     private static readonly AdoGenType GenType = AdoGenType.NpgsqlBulkModel;
//     private static readonly TestTypes TestType = TestTypes.User;
//     
//     [Fact]
//     public void Diagnostics_ShouldBeEmpty_WhenValid() => 
//         GenType.RunUserGenerator(TestType).Diagnostics.Should().BeEmpty();
//
//     [Fact]
//     public void MapperFile_ShouldBeGenerated() =>
//         GenType.GenerateFile(TestType, AdoGenType.NpgsqlMapper).Should().NotBeEmpty();
//     
//     [Fact]
//     public void DomainFile_ShouldBeGenerated()
//     {
//         GenType.GenerateFile(TestType, AdoGenType.NpgsqlDomainModel).Should().NotBeEmpty();
//     }
//
//     [Fact]
//     public Task BulkFile_ShouldMatchSnapshot() => Verify(GenType.GenerateFile(TestType, GenType));
// }
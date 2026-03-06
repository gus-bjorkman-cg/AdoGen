// namespace AdoGen.Generator.Tests.PostgreSql;
//
// public class NpgsqlDomainTests
// {
//     private static readonly AdoGenType GenType = AdoGenType.NpgsqlDomainModel;
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
//     public Task DomainFile_ShouldMatchSnapshot() => Verify(GenType.GenerateFile(TestType, GenType));
//     
//     [Fact]
//     public void BulkFile_ShouldNotBeGenerated() =>
//         GenType.GenerateFile(TestType, AdoGenType.NpgsqlBulkModel).Should().BeEmpty();
// }
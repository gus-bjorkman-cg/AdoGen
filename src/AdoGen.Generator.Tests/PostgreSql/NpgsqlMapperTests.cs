// namespace AdoGen.Generator.Tests.PostgreSql;
//
// public class NpgsqlMapperTests
// {
//     private static readonly AdoGenType GenType = AdoGenType.NpgsqlMapper;
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
//         GenType.GenerateFile(TestType, AdoGenType.NpgsqlDomainModel).Should().BeEmpty();
//     
//     [Fact]
//     public void BulkFile_ShouldNotBeGenerated() =>
//         GenType.GenerateFile(TestType, AdoGenType.NpgsqlBulkModel).Should().BeEmpty();
// }
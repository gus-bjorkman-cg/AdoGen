namespace AdoGen.Generator.Tests;

public sealed class MapperTests
{
    public static TheoryData<AdoGenType, TestTypes> Cases => new()
    {
        { AdoGenType.SqlMapper, TestTypes.User },
        { AdoGenType.SqlMapper, TestTypes.AuditEvent },
        { AdoGenType.SqlMapper, TestTypes.TestType },

        { AdoGenType.NpgsqlMapper, TestTypes.User },
        { AdoGenType.NpgsqlMapper, TestTypes.AuditEvent },
        { AdoGenType.NpgsqlMapper, TestTypes.TestType },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Diagnostics_ShouldBeEmpty(AdoGenType genType, TestTypes testType) =>
        genType.RunUserGenerator(testType).Diagnostics.Should().BeEmpty();

    [Theory]
    [MemberData(nameof(Cases))]
    public Task MapperFile_ShouldMatchSnapshot(AdoGenType genType, TestTypes testType) =>
        Verify(genType.GenerateFile(testType, genType.GetMapperType()))
            .UseTextForParameters($"{testType.Name}.{genType.Provider.Name}");

    [Theory]
    [MemberData(nameof(Cases))]
    public void DomainFile_ShouldBeEmpty(AdoGenType genType, TestTypes testType) =>
        genType.GenerateFile(testType, genType.GetDomainType()).Should().BeEmpty();

    [Theory]
    [MemberData(nameof(Cases))]
    public void BulkFile_ShouldBeEmpty(AdoGenType genType, TestTypes testType) =>
        genType.GenerateFile(testType, genType.GetBulkType()).Should().BeEmpty();
}
namespace AdoGen.Generator.Tests;

public sealed class DomainTests
{
    public static TheoryData<AdoGenType, TestTypes> Cases => new()
    {
        { AdoGenType.SqlDomainModel, TestTypes.User },
        { AdoGenType.SqlDomainModel, TestTypes.AuditEvent },
        { AdoGenType.SqlDomainModel, TestTypes.TestType },

        { AdoGenType.NpgsqlDomainModel, TestTypes.User },
        { AdoGenType.NpgsqlDomainModel, TestTypes.AuditEvent },
        { AdoGenType.NpgsqlDomainModel, TestTypes.TestType },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Diagnostics_ShouldBeEmpty(AdoGenType genType, TestTypes testType) =>
        genType.RunUserGenerator(testType).Diagnostics.Should().BeEmpty();

    [Theory]
    [MemberData(nameof(Cases))]
    public void MapperFile_ShouldBeGenerated(AdoGenType genType, TestTypes testType) =>
        genType.GenerateFile(testType, genType.GetMapperType()).Should().NotBeEmpty();

    [Theory]
    [MemberData(nameof(Cases))]
    public Task DomainFile_ShouldMatchSnapshot(AdoGenType genType, TestTypes testType) =>
        Verify(genType.GenerateFile(testType, genType.GetDomainType()))
            .UseTextForParameters($"{testType.Name}.{genType.Provider.Name}");

    [Theory]
    [MemberData(nameof(Cases))]
    public void BulkFile_ShouldBeEmpty(AdoGenType genType, TestTypes testType) =>
        genType.GenerateFile(testType, genType.GetBulkType()).Should().BeEmpty();
}
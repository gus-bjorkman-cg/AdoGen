namespace AdoGen.Generator.Tests;

public sealed class BulkTests
{
    public static TheoryData<AdoGenType, TestTypes> Cases => new()
    {
        { AdoGenType.SqlBulkModel, TestTypes.User },
        { AdoGenType.SqlBulkModel, TestTypes.AuditEvent },
        { AdoGenType.SqlBulkModel, TestTypes.TestType },
        { AdoGenType.SqlBulkModel, TestTypes.VersionedOrder },
        { AdoGenType.SqlBulkModel, TestTypes.VersionedOrderGuid },

        { AdoGenType.NpgsqlBulkModel, TestTypes.User },
        { AdoGenType.NpgsqlBulkModel, TestTypes.AuditEvent },
        { AdoGenType.NpgsqlBulkModel, TestTypes.TestType },
        { AdoGenType.NpgsqlBulkModel, TestTypes.VersionedOrder },
        { AdoGenType.NpgsqlBulkModel, TestTypes.VersionedOrderGuid },
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
    public void DomainFile_ShouldBeGenerated(AdoGenType genType, TestTypes testType) =>
        genType.GenerateFile(testType, genType.GetDomainType()).Should().NotBeEmpty();

    [Theory]
    [MemberData(nameof(Cases))]
    public Task BulkFile_ShouldMatchSnapshot(AdoGenType genType, TestTypes testType) =>
        Verify(genType.GenerateFile(testType, genType))
            .UseTextForParameters($"{testType.Name}.{genType.Provider.Name}");
}


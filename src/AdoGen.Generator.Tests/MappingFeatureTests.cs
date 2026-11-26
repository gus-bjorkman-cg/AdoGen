namespace AdoGen.Generator.Tests;

/// <summary>
/// Mapping and profile features from docs/analysis/FEATURES.md section B: Ignore (F11), enum AsString (F14),
/// unsigned/char defaults (F15), nested DTOs (F17), naming attribute (F19) and struct DTOs (F20).
/// </summary>
public sealed class MappingFeatureTests
{
    private sealed record ProviderSetup(
        string Usings,
        string Ext,
        string Mapper,
        string Domain,
        string Bulk,
        string Profile,
        string Naming,
        AdoGenType MapperType,
        AdoGenType DomainType,
        AdoGenType BulkType);

    private static ProviderSetup Setup(string provider) =>
        provider == "SqlServer"
            ? new ProviderSetup("using System;\nusing AdoGen.SqlServer;\nnamespace Test;\n", "Sql",
                "ISqlMapper", "ISqlDomainModel", "ISqlBulkModel", "SqlProfile", "SqlNaming",
                AdoGenType.SqlMapper, AdoGenType.SqlDomainModel, AdoGenType.SqlBulkModel)
            : new ProviderSetup("using System;\nusing AdoGen.PostgreSql;\nnamespace Test;\n", "Npgsql",
                "INpgsqlMapper", "INpgsqlDomainModel", "INpgsqlBulkModel", "NpgsqlProfile", "NpgsqlNaming",
                AdoGenType.NpgsqlMapper, AdoGenType.NpgsqlDomainModel, AdoGenType.NpgsqlBulkModel);

    // ── F15: unsigned / char defaults ─────────────────────────────────────────

    [Theory]
    [InlineData("SqlServer", "[Huge] DECIMAL(20,0) NOT NULL", "[Letter] NCHAR(1) NOT NULL", "[Medium] BIGINT NOT NULL")]
    [InlineData("PostgreSql", "\"Huge\" NUMERIC(20,0) NOT NULL", "\"Letter\" CHAR(1) NOT NULL", "\"Medium\" BIGINT NOT NULL")]
    public void UnsignedAndChar_ShouldMapByDefault_WhenNoTypeConfigured(string provider, string huge, string letter, string medium)
    {
        // Arrange
        var p = Setup(provider);
        var source = p.Usings + $$"""

            public enum Big : ulong { A = 0, B = ulong.MaxValue }
            public sealed partial record Counter(Guid Id, sbyte Tiny, ushort Small, uint Medium, ulong Huge, char Letter, uint? MaybeMedium, Big Kind, Big? MaybeKind) : {{p.Bulk}};
            public sealed class CounterProfile : {{p.Profile}}<Counter>
            {
                public CounterProfile() { }
            }
            """;

        // Act
        var result = source.RunGenerator(p.BulkType);
        var domain = result.Result.GetGeneratedText($"Counter.Domain.{p.Ext}.g.cs");
        var errors = source.GetOutputCompilationErrors(p.BulkType);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        errors.Should().BeEmpty();
        domain.Should().Contain(huge).And.Contain(letter).And.Contain(medium);
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void Unsigned_ShouldConvertThroughWireType_WhenReadingAndWriting(string provider)
    {
        // Arrange
        var p = Setup(provider);
        var source = p.Usings + $$"""

            public sealed partial record Counter(Guid Id, uint Medium, ulong? Huge) : {{p.Mapper}};
            """;

        // Act
        var result = source.RunGenerator(p.MapperType);
        var mapper = result.Result.GetGeneratedText($"Counter.Mapper.{p.Ext}.g.cs");

        // Assert
        result.Diagnostics.Should().BeEmpty();
        mapper.Should().Contain("(uint)reader.GetInt64(")
            .And.Contain("(ulong)reader.GetDecimal(")
            .And.Contain("Value = (long)value,")
            .And.Contain("DBNull.Value : (decimal)value.Value,");
    }

    // ── F14: enum AsString ────────────────────────────────────────────────────

    [Theory]
    [InlineData("SqlServer", "[Status] VARCHAR(20) NOT NULL", "[Previous] VARCHAR(20) NULL")]
    [InlineData("PostgreSql", "\"Status\" VARCHAR(20) NOT NULL", "\"Previous\" VARCHAR(20) NULL")]
    public void AsString_ShouldStoreEnumAsVarChar_WhenConfigured(string provider, string status, string previous)
    {
        // Arrange
        var p = Setup(provider);
        var source = p.Usings + $$"""

            public enum OrderStatus { Pending, Shipped, Delivered, Done = Delivered }
            public sealed partial record Order(Guid Id, OrderStatus Status, OrderStatus? Previous) : {{p.Bulk}};
            public sealed class OrderProfile : {{p.Profile}}<Order>
            {
                public OrderProfile()
                {
                    RuleFor(x => x.Status).AsString(20);
                    RuleFor(x => x.Previous).AsString(20);
                }
            }
            """;

        // Act
        var result = source.RunGenerator(p.BulkType);
        var domain = result.Result.GetGeneratedText($"Order.Domain.{p.Ext}.g.cs");
        var mapper = result.Result.GetGeneratedText($"Order.Mapper.{p.Ext}.g.cs");
        var errors = source.GetOutputCompilationErrors(p.BulkType);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        errors.Should().BeEmpty();
        domain.Should().Contain(status).And.Contain(previous);
        mapper.Should().Contain("public static string StatusToDb(")
            .And.Contain("\"Shipped\" => global::Test.OrderStatus.Shipped,")
            .And.Contain($"Order{p.Ext}.StatusFromDb(reader.GetString(")
            .And.NotContain("\"Done\"");
    }

    [Theory]
    [InlineData("SqlServer", "int Status")]
    [InlineData("PostgreSql", "int Status")]
    [InlineData("SqlServer", "Perms Status")]
    [InlineData("PostgreSql", "Perms Status")]
    public void AG023_ShouldBeEmitted_WhenAsStringTargetsNonEnumOrFlags(string provider, string property)
    {
        // Arrange
        var p = Setup(provider);
        var source = p.Usings + $$"""

            [Flags] public enum Perms { None = 0, Read = 1, Write = 2 }
            public sealed partial record Order(Guid Id, {{property}}) : {{p.Domain}};
            public sealed class OrderProfile : {{p.Profile}}<Order>
            {
                public OrderProfile() => RuleFor(x => x.Status).AsString(20);
            }
            """;

        // Act
        var result = source.RunGenerator(p.DomainType);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG023");
    }

    // ── F11: Ignore ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void Ignore_ShouldExcludeProperty_WhenConfigured(string provider)
    {
        // Arrange
        var p = Setup(provider);
        var source = p.Usings + $$"""

            public sealed partial record Person(Guid Id, int Age, string DisplayName) : {{p.Bulk}}
            {
                public string? Cache { get; set; }
            }
            public sealed class PersonProfile : {{p.Profile}}<Person>
            {
                public PersonProfile()
                {
                    RuleFor(x => x.DisplayName).Ignore();
                    RuleFor(x => x.Cache).Ignore();
                }
            }
            """;

        // Act
        var result = source.RunGenerator(p.BulkType);
        var domain = result.Result.GetGeneratedText($"Person.Domain.{p.Ext}.g.cs");
        var mapper = result.Result.GetGeneratedText($"Person.Mapper.{p.Ext}.g.cs");
        var errors = source.GetOutputCompilationErrors(p.BulkType);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        errors.Should().BeEmpty();
        domain.Should().NotContain("DisplayName").And.NotContain("Cache");
        mapper.Should().Contain("DisplayName: default!").And.NotContain("Cache");
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void AG022_ShouldBeEmitted_WhenIgnoredPropertyIsKey(string provider)
    {
        // Arrange
        var p = Setup(provider);
        var source = p.Usings + $$"""

            public sealed partial record Person(Guid Id, int Age) : {{p.Domain}};
            public sealed class PersonProfile : {{p.Profile}}<Person>
            {
                public PersonProfile()
                {
                    Key(x => x.Id);
                    RuleFor(x => x.Id).Ignore();
                }
            }
            """;

        // Act
        var result = source.RunGenerator(p.DomainType);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG022");
    }

    // ── F17: nested DTOs ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void Output_ShouldCompile_WhenDtoIsNested(string provider)
    {
        // Arrange
        var p = Setup(provider);
        var source = p.Usings + $$"""

            public static partial class Outer
            {
                internal partial class Middle
                {
                    public sealed partial record Item(Guid Id, int Qty) : {{p.Bulk}};
                    public sealed class ItemProfile : {{p.Profile}}<Item>
                    {
                        public ItemProfile() { }
                    }
                }
            }
            """;

        // Act
        var result = source.RunGenerator(p.BulkType);
        var domain = result.Result.GetGeneratedText($"Outer.Middle.Item.Domain.{p.Ext}.g.cs");
        var errors = source.GetOutputCompilationErrors(p.BulkType);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        errors.Should().BeEmpty();
        domain.Should().Contain($"internal static class OuterMiddleItem{p.Ext}PatchExtensions")
            .And.Contain("partial class Outer");
    }

    // ── F19: naming attribute ─────────────────────────────────────────────────

    [Theory]
    [InlineData("SqlServer", "[dbo].[Item]")]
    [InlineData("PostgreSql", "\"public\".\"Item\"")]
    public void NamingAttribute_ShouldDisablePluralization_WhenPluralizeIsFalse(string provider, string expectedTable)
    {
        // Arrange
        var p = Setup(provider);
        var source = p.Usings.Replace("namespace Test;", $"[assembly: {p.Naming}(Pluralize = false)]\nnamespace Test;") + $$"""

            public sealed partial record Item(Guid Id, int Qty) : {{p.Domain}};
            public sealed class ItemProfile : {{p.Profile}}<Item>
            {
                public ItemProfile() { }
            }
            """;

        // Act
        var result = source.RunGenerator(p.DomainType);
        var domain = result.Result.GetGeneratedText($"Item.Domain.{p.Ext}.g.cs");

        // Assert
        result.Diagnostics.Should().BeEmpty();
        domain.Should().Contain(expectedTable).And.NotContain(expectedTable.Replace("Item", "Items"));
    }

    // ── F20: struct DTOs ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void Output_ShouldCompile_WhenMapperDtoIsStruct(string provider)
    {
        // Arrange
        var p = Setup(provider);
        var source = p.Usings + $$"""

            public readonly partial record struct Point(int X, int Y) : {{p.Mapper}};
            public partial struct Row : {{p.Mapper}}
            {
                public Guid Id { get; set; }
                public decimal Amount { get; set; }
            }
            """;

        // Act
        var result = source.RunGenerator(p.MapperType);
        var point = result.Result.GetGeneratedText($"Point.Mapper.{p.Ext}.g.cs");
        var row = result.Result.GetGeneratedText($"Row.Mapper.{p.Ext}.g.cs");
        var errors = source.GetOutputCompilationErrors(p.MapperType);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        errors.Should().BeEmpty();
        point.Should().Contain("public partial record struct Point");
        row.Should().Contain("public partial struct Row");
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void AG024_ShouldBeEmitted_WhenStructIsDomainModel(string provider)
    {
        // Arrange
        var p = Setup(provider);
        var source = p.Usings + $$"""

            public partial record struct Point(Guid Id, int X) : {{p.Domain}};
            public sealed class PointProfile : {{p.Profile}}<Point>
            {
                public PointProfile() { }
            }
            """;

        // Act
        var result = source.RunGenerator(p.DomainType);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG024");
    }
}

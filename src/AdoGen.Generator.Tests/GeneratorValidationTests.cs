namespace AdoGen.Generator.Tests;

/// <summary>
/// Diagnostics and "generated code compiles" regressions for DTO/profile shapes (docs/analysis/BUGS.md B10–B19,
/// FEATURES.md F1–F10). Every reachable AGxxx descriptor has at least one test here or in DomainTests.
/// </summary>
public sealed class GeneratorValidationTests
{
    private const string SqlUsings = """
                                     using System;
                                     using AdoGen.SqlServer;
                                     namespace Test;
                                     """;

    private const string NpgsqlUsings = """
                                        using System;
                                        using AdoGen.PostgreSql;
                                        namespace Test;
                                        """;

    [Fact]
    public void Output_ShouldCompile_WhenDomainModelIsValid()
    {
        // Arrange
        var source = SqlUsings + """

            public sealed partial record Item(Guid Id, string Name, int Qty) : ISqlBulkModel;
            public sealed class ItemProfile : SqlProfile<Item>
            {
                public ItemProfile() => RuleFor(x => x.Name).VarChar(50);
            }
            """;

        // Act
        var errors = source.GetOutputCompilationErrors(AdoGenType.SqlBulkModel);

        // Assert
        errors.Should().BeEmpty();
    }

    // ── B10: no forced `sealed`; inherited markers are not rediscovered ───────

    [Fact]
    public void Output_ShouldCompile_WhenDtoIsNotSealedAndHasSubclass()
    {
        // Arrange
        var source = SqlUsings + """

            public partial class Animal : ISqlMapper
            {
                public int Id { get; set; }
                public int Legs { get; set; }
            }
            public class Dog : Animal { }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.SqlMapper);
        var errors = source.GetOutputCompilationErrors(AdoGenType.SqlMapper);

        // Assert
        result.Diagnostics.Should().BeEmpty();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void AG014_ShouldBeEmitted_WhenDtoIsAbstract()
    {
        // Arrange
        var source = SqlUsings + """

            public abstract partial class Shape : ISqlMapper
            {
                public int Id { get; set; }
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.SqlMapper);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG014");
    }

    // ── B11 / F17: nested types need partial, non-generic, public/internal containers ──

    [Fact]
    public void AG015_ShouldBeEmitted_WhenContainingTypeIsGeneric()
    {
        // Arrange
        var source = SqlUsings + """

            public static partial class Outer<T>
            {
                public partial record Inner(int Id, int Value) : ISqlMapper;
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.SqlMapper);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG015");
    }

    [Fact]
    public void AG015_ShouldBeEmitted_WhenContainingTypeIsNotPartial()
    {
        // Arrange
        var source = SqlUsings + """

            public static class Outer
            {
                public partial record Inner(int Id, int Value) : ISqlMapper;
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.SqlMapper);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG015");
    }

    // ── B12: patch accessibility follows the DTO ──────────────────────────────

    [Fact]
    public void Output_ShouldCompile_WhenInternalDtoHasInternalMemberType()
    {
        // Arrange
        var source = SqlUsings + """

            internal enum Color { Red, Green }
            internal sealed partial record Paint(Guid Id, Color Color) : ISqlDomainModel;
            internal sealed class PaintProfile : SqlProfile<Paint>
            {
                public PaintProfile() { }
            }
            """;

        // Act
        var errors = source.GetOutputCompilationErrors(AdoGenType.SqlDomainModel);

        // Assert
        errors.Should().BeEmpty();
    }

    // ── B13: computed / get-only properties and indexers are not columns ──────

    [Fact]
    public void Output_ShouldCompile_WhenDtoHasComputedPropertyAndIndexer()
    {
        // Arrange
        var source = NpgsqlUsings + """

            public sealed partial record Line(Guid Id, int Qty, int Price) : INpgsqlDomainModel
            {
                public int Total => Qty * Price;
                public int this[int i] => i;
            }
            public sealed class LineProfile : NpgsqlProfile<Line>
            {
                public LineProfile() { }
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.NpgsqlDomainModel);
        var errors = source.GetOutputCompilationErrors(AdoGenType.NpgsqlDomainModel);
        var domain = result.Result.GetGeneratedText("Line.Domain.Npgsql.g.cs");

        // Assert
        result.Diagnostics.Should().BeEmpty();
        errors.Should().BeEmpty();
        domain.Should().NotContain("\\\"Total\\\"");
    }

    // ── B14: invalid .Name(...) ───────────────────────────────────────────────

    [Fact]
    public void AG016_ShouldBeEmitted_WhenColumnNameIsNotAnIdentifier()
    {
        // Arrange
        var source = SqlUsings + """

            public sealed partial record Stock(Guid Id, int Count) : ISqlDomainModel;
            public sealed class StockProfile : SqlProfile<Stock>
            {
                public StockProfile() => RuleFor(x => x.Count).Name("Item Count");
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.SqlDomainModel);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG016");
    }

    // ── B15: unsupported CLR types / TimeSpan ─────────────────────────────────

    [Fact]
    public void AG017_ShouldBeEmitted_WhenPropertyTypeHasNoDefaultMapping()
    {
        // Arrange
        var source = SqlUsings + """

            public sealed partial record Counter(Guid Id, Version Hits) : ISqlDomainModel;
            public sealed class CounterProfile : SqlProfile<Counter>
            {
                public CounterProfile() { }
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.SqlDomainModel);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG017");
    }

    [Theory]
    [InlineData("SqlServer", "[Duration] TIME NOT NULL")]
    [InlineData("PostgreSql", "\"Duration\" interval NOT NULL")]
    public void CreateTable_ShouldMapTimeSpan_WhenNoTypeConfigured(string provider, string expectedColumn)
    {
        // Arrange
        var isSql = provider == "SqlServer";
        var genType = isSql ? AdoGenType.SqlDomainModel : AdoGenType.NpgsqlDomainModel;
        var source = (isSql ? SqlUsings : NpgsqlUsings) + $$"""

            public sealed partial record Job(Guid Id, TimeSpan Duration) : {{(isSql ? "ISqlDomainModel" : "INpgsqlDomainModel")}};
            public sealed class JobProfile : {{(isSql ? "SqlProfile" : "NpgsqlProfile")}}<Job>
            {
                public JobProfile() { }
            }
            """;

        // Act
        var result = source.RunGenerator(genType);
        var domain = result.Result.GetGeneratedText($"Job.Domain.{genType.Provider.ExtensionName}.g.cs");

        // Assert
        result.Diagnostics.Should().BeEmpty();
        domain.Should().Contain(expectedColumn);
    }

    // ── B16: duplicate profiles ───────────────────────────────────────────────

    [Fact]
    public void AG018_ShouldBeEmitted_WhenTypeHasTwoProfilesForSameProvider()
    {
        // Arrange
        var source = SqlUsings + """

            public sealed partial record Dup(Guid Id, string Name) : ISqlDomainModel;
            public sealed class DupProfileA : SqlProfile<Dup>
            {
                public DupProfileA() => RuleFor(x => x.Name).VarChar(10);
            }
            public sealed class DupProfileB : SqlProfile<Dup>
            {
                public DupProfileB() => RuleFor(x => x.Name).NVarChar(99);
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.SqlDomainModel);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG018");
    }

    // ── B17: malformed profile syntax ─────────────────────────────────────────

    [Fact]
    public void AG019_ShouldBeEmitted_WhenKeySelectorIsNotALambda()
    {
        // Arrange
        var source = SqlUsings + """

            using System.Linq.Expressions;
            public sealed partial record Thing(Guid Code, int Value) : ISqlDomainModel;
            public sealed class ThingProfile : SqlProfile<Thing>
            {
                private static readonly Expression<Func<Thing, Guid>> Selector = x => x.Code;
                public ThingProfile() => Key(Selector);
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.SqlDomainModel);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG019");
        result.Diagnostics.Should().NotContain(d => d.Id == "CS8785");
    }

    [Fact]
    public void Profile_ShouldBeParsed_WhenCallsUseThisQualifier()
    {
        // Arrange
        var source = SqlUsings + """

            public sealed partial record Note(Guid Id, string Text) : ISqlDomainModel;
            public sealed class NoteProfile : SqlProfile<Note>
            {
                public NoteProfile()
                {
                    this.Table("MyNotes");
                    this.RuleFor(x => x.Text).VarChar(30);
                }
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.SqlDomainModel);
        var domain = result.Result.GetGeneratedText("Note.Domain.Sql.g.cs");

        // Assert
        result.Diagnostics.Should().BeEmpty();
        domain.Should().Contain("[dbo].[MyNotes]").And.Contain("VARCHAR(30)");
    }

    [Fact]
    public void AG007_ShouldBeEmitted_WhenTableNameIsNotConstant()
    {
        // Arrange
        var source = SqlUsings + """

            public sealed partial record Cfg(Guid Id) : ISqlDomainModel;
            public sealed class CfgProfile : SqlProfile<Cfg>
            {
                private static readonly string Name = "Configs";
                public CfgProfile() => Table(Name);
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.SqlDomainModel);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG007");
    }

    // ── B19: AG002 message names the provider-specific profile ────────────────

    [Theory]
    [InlineData("SqlServer", "SqlProfile")]
    [InlineData("PostgreSql", "NpgsqlProfile")]
    public void AG002_ShouldNameProviderProfile_WhenProfileMissing(string provider, string profileName)
    {
        // Arrange
        var isSql = provider == "SqlServer";
        var genType = isSql ? AdoGenType.SqlDomainModel : AdoGenType.NpgsqlDomainModel;
        var source = (isSql ? SqlUsings : NpgsqlUsings) + $$"""

            public sealed partial record Gadget(Guid Id) : {{(isSql ? "ISqlDomainModel" : "INpgsqlDomainModel")}};
            """;

        // Act
        var result = source.RunGenerator(genType);

        // Assert
        var messages = result.Diagnostics
            .Where(d => d.Id == "AG002")
            .Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture))
            .ToList();
        messages.Should().NotBeEmpty()
            .And.OnlyContain(m => m == $"Type 'Gadget' has no {profileName}. Create a class deriving from {profileName}<Gadget>.");
    }

    // ── F9: one test per descriptor ───────────────────────────────────────────

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void AG001_ShouldBeEmitted_WhenDtoIsNotPartial(string provider)
    {
        // Arrange
        var (usings, genType, mapper, _) = Provider(provider);
        var source = usings + $$"""

            public sealed record Plain(int Id) : {{mapper}};
            """;

        // Act
        var result = source.RunGenerator(genType);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG001");
    }

    [Fact]
    public void AG004_ShouldBeEmitted_WhenSqlServerStringHasNoTypeAndSize()
    {
        // Arrange
        var source = SqlUsings + """

            public sealed partial record Tag(Guid Id, string Label) : ISqlDomainModel;
            public sealed class TagProfile : SqlProfile<Tag>
            {
                public TagProfile() { }
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.SqlDomainModel);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG004");
    }

    [Fact]
    public void AG004_ShouldBeEmitted_WhenNpgsqlVarcharHasNoSize()
    {
        // Arrange
        var source = NpgsqlUsings + """

            using NpgsqlTypes;
            public sealed partial record Tag(Guid Id, string Label) : INpgsqlDomainModel;
            public sealed class TagProfile : NpgsqlProfile<Tag>
            {
                public TagProfile() => RuleFor(x => x.Label).Type(NpgsqlDbType.Varchar);
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.NpgsqlDomainModel);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG004");
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void AG005_ShouldBeEmitted_WhenDecimalHasNoPrecisionAndScale(string provider)
    {
        // Arrange
        var (usings, _, _, (genType, domain, profile)) = Provider(provider);
        var source = usings + $$"""

            public sealed partial record Price(Guid Id, decimal Amount) : {{domain}};
            public sealed class PriceProfile : {{profile}}<Price>
            {
                public PriceProfile() { }
            }
            """;

        // Act
        var result = source.RunGenerator(genType);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG005");
    }

    [Fact]
    public void AG006_ShouldBeEmitted_WhenSqlServerBinaryHasNoTypeAndSize()
    {
        // Arrange
        var source = SqlUsings + """

            public sealed partial record Blob(Guid Id, byte[] Data) : ISqlDomainModel;
            public sealed class BlobProfile : SqlProfile<Blob>
            {
                public BlobProfile() { }
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.SqlDomainModel);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG006");
    }

    [Fact]
    public void AG006_ShouldBeEmitted_WhenNpgsqlBinaryIsNotBytea()
    {
        // Arrange
        var source = NpgsqlUsings + """

            using NpgsqlTypes;
            public sealed partial record Blob(Guid Id, byte[] Data) : INpgsqlDomainModel;
            public sealed class BlobProfile : NpgsqlProfile<Blob>
            {
                public BlobProfile() => RuleFor(x => x.Data).Type(NpgsqlDbType.Text);
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.NpgsqlDomainModel);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG006");
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void AG008_ShouldBeEmitted_WhenDomainModelHasNoKey(string provider)
    {
        // Arrange
        var (usings, _, _, (genType, domain, profile)) = Provider(provider);
        var source = usings + $$"""

            public sealed partial record Event(Guid Code, int Value) : {{domain}};
            public sealed class EventProfile : {{profile}}<Event>
            {
                public EventProfile() { }
            }
            """;

        // Act
        var result = source.RunGenerator(genType);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG008");
    }

    [Fact]
    public void AG010_ShouldBeEmitted_WhenDtoIsStatic()
    {
        // Arrange
        var source = SqlUsings + """

            public static partial class Holder : ISqlMapper
            {
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.SqlMapper);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG010");
    }

    [Fact]
    public void AG011_ShouldBeEmitted_WhenDtoIsPrivate()
    {
        // Arrange
        var source = SqlUsings + """

            public partial class Outer
            {
                private partial record Hidden(int Id) : ISqlMapper;
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.SqlMapper);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG011");
    }

    // ── F2: key-only models ───────────────────────────────────────────────────

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void AG020_ShouldBeReportedAsInfoAndStillGenerate_WhenEveryColumnIsKey(string provider)
    {
        // Arrange
        var (usings, _, _, (genType, domain, profile)) = Provider(provider);
        var source = usings + $$"""

            public sealed partial record Link(Guid LeftId, Guid RightId) : {{domain}};
            public sealed class LinkProfile : {{profile}}<Link>
            {
                public LinkProfile()
                {
                    Key(x => x.LeftId);
                    Key(x => x.RightId);
                }
            }
            """;

        // Act
        var result = source.RunGenerator(genType);
        var errors = source.GetOutputCompilationErrors(genType);
        var generated = result.Result.GetGeneratedText($"Link.Domain.{genType.Provider.ExtensionName}.g.cs");

        // Assert
        result.Diagnostics.Should().OnlyContain(d => d.Id == "AG020" && d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Info);
        result.Diagnostics.Should().NotBeEmpty();
        errors.Should().BeEmpty();
        generated.Should().Contain("UpsertAsync");
    }

    [Fact]
    public void AG020_ShouldBeReported_WhenNonKeyColumnsAreReadOnlyOrIdentity()
    {
        // Arrange
        var source = SqlUsings + """

            public sealed partial record Stamp(int Seq, Guid Code, DateTime CreatedAt) : ISqlDomainModel;
            public sealed class StampProfile : SqlProfile<Stamp>
            {
                public StampProfile()
                {
                    Identity(x => x.Seq);
                    Key(x => x.Code);
                    RuleFor(x => x.CreatedAt).ReadOnly().DefaultValue("SYSUTCDATETIME()");
                }
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.SqlDomainModel);

        // Assert
        result.Diagnostics.Should().Contain(d => d.Id == "AG020");
    }

    [Theory]
    [InlineData("Count")]
    [InlineData("Version")]
    public void AG020_ShouldNotBeReported_WhenModelHasWritableNonKeyColumn(string configuredColumn)
    {
        // Arrange
        var source = SqlUsings + $$"""

            public sealed partial record Counter(Guid Id, int Count, int Version) : ISqlDomainModel;
            public sealed class CounterProfile : SqlProfile<Counter>
            {
                public CounterProfile()
                {
                    RuleFor(x => x.{{(configuredColumn == "Count" ? "Version" : "Count")}}).ReadOnly();
                    RuleFor(x => x.{{configuredColumn}}){{(configuredColumn == "Version" ? ".ConcurrencyToken()" : "")}};
                }
            }
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.SqlDomainModel);

        // Assert
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void AG020_ShouldNotBeReported_WhenDtoIsMapperOnly()
    {
        // Arrange
        var source = SqlUsings + """

            public sealed partial record Row(Guid Id) : ISqlMapper;
            """;

        // Act
        var result = source.RunGenerator(AdoGenType.SqlMapper);

        // Assert
        result.Diagnostics.Should().BeEmpty();
    }

    // ── F3: unknown chain methods ─────────────────────────────────────────────

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void AG021_ShouldBeEmitted_WhenChainContainsUnknownMethod(string provider)
    {
        // Arrange
        var (usings, _, _, (genType, domain, profile)) = Provider(provider);
        var source = usings + $$"""

            public static class PropertyBuilderExtensions
            {
                public static PropertyBuilder<T> Indexed<T>(this PropertyBuilder<T> builder) => builder;
            }
            public sealed partial record Widget(Guid Id, int Size) : {{domain}};
            public sealed class WidgetProfile : {{profile}}<Widget>
            {
                public WidgetProfile() => RuleFor(x => x.Size).Indexed();
            }
            """;

        // Act
        var result = source.RunGenerator(genType);

        // Assert
        var messages = result.Diagnostics
            .Where(d => d.Id == "AG021")
            .Select(d => d.GetMessage(System.Globalization.CultureInfo.InvariantCulture))
            .ToList();
        messages.Should().NotBeEmpty()
            .And.OnlyContain(m => m.Contains("'Indexed'") && m.Contains("'Size'") && m.Contains("'Widget'"));
    }

    private static (string Usings, AdoGenType MapperType, string MapperInterface, (AdoGenType DomainType, string DomainInterface, string ProfileBase) Domain)
        Provider(string provider) =>
        provider == "SqlServer"
            ? (SqlUsings, AdoGenType.SqlMapper, "ISqlMapper", (AdoGenType.SqlDomainModel, "ISqlDomainModel", "SqlProfile"))
            : (NpgsqlUsings, AdoGenType.NpgsqlMapper, "INpgsqlMapper", (AdoGenType.NpgsqlDomainModel, "INpgsqlDomainModel", "NpgsqlProfile"));
}

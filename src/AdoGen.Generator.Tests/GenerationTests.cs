namespace AdoGen.Generator.Tests;

public class GenerationTests
{
    [Fact]
    public Task SqlResult_ShouldRenderCorrectly()
    {
        var (result, diagnostics) = Source.RunGenerator(ProviderKind.SqlServer);
        
        if (diagnostics.Length > 0)
        {
            var formatted = string.Join(Environment.NewLine, diagnostics.Select(d => d.ToString()));
            Assert.Fail("No sources were generated.\nDiagnostics:\n" + formatted);
        }
        
        var generated = result.Results
            .SelectMany(r => r.GeneratedSources)
            .Select(s => new
            {
                s.HintName,
                Source = s.SourceText.ToString()
            })
            .OrderBy(x => x.HintName, StringComparer.Ordinal)
            .ToArray();
        
        return Verify(generated);
    }

    private const string Source =
        """
        using System;
        using AdoGen.SqlServer;
        
        namespace AdoGen.Generator.Tests;
        
        public sealed partial record User(Guid Id, string Name, string Email) : ISqlBulkModel;
        
        public sealed class UserProfile : SqlProfile<User>
        {
            public UserProfile()
            {
                RuleFor(x => x.Name).VarChar(20);
                RuleFor(x => x.Email).VarChar(50);
            }
        }
        """;

    [Fact]
    public Task NpgsqlResult_ShouldRenderCorrectly()
    {
        var (result, diagnostics) = PostgreSource.RunGenerator(ProviderKind.PostgreSql);
        if (diagnostics.Length > 0)
        {
            var formatted = string.Join(Environment.NewLine, diagnostics.Select(d => d.ToString()));
            Assert.Fail("No sources were generated.\nDiagnostics:\n" + formatted);
        }

        var generated = result.Results
            .SelectMany(r => r.GeneratedSources)
            .Select(s => new { s.HintName, Source = s.SourceText.ToString() })
            .OrderBy(x => x.HintName, StringComparer.Ordinal)
            .ToArray();

        return Verify(generated);
    }

    private const string PostgreSource =
        """
        using System;
        using AdoGen.PostgreSql;
        using NpgsqlTypes;

        namespace AdoGen.Generator.Tests;

        public sealed partial record UserPg(Guid Id, string Name, string Email) : INpgsqlBulkModel;

        public sealed class UserPgProfile : NpgsqlProfile<UserPg>
        {
            public UserPgProfile()
            {
                RuleFor(x => x.Name).Varchar(20);
                RuleFor(x => x.Email).Varchar(50);
            }
        }
        """;
}
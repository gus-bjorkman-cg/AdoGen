using System;
using System.IO;
using System.Linq;
using System.Text;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    OnPushBranches = ["**"],
    OnPullRequestBranches = ["**"],
    OnPushTags = ["v*"],
    InvokedTargets = [nameof(CI)], 
    ImportSecrets = [nameof(NuGetApiKey)],
    CacheKeyFiles = ["**/global.json", "**/*.csproj", "**/Directory.Packages.props"])]
class Build : NukeBuild
{
    const string TagPath = "refs/tags/";
    const string LocalBuild = "0.0.0-localbuild";
    const string NugetSource = "https://api.nuget.org/v3/index.json";
    
    [Solution(GenerateProjects = true)]
    readonly Solution Solution;
    
    [GitRepository] 
    readonly GitRepository GitRepo;
    
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    
    [Parameter("NuGet API key (from GH secret or env NUGET_API_KEY)")]
    [Secret]
    readonly string NuGetApiKey;
    
    static AbsolutePath OutputDirectory => RootDirectory / "output";
    static AbsolutePath SourceDirectory => RootDirectory / "src";
    static AbsolutePath Changelog => RootDirectory / "Changelog.md";
    
    Project SqlServerProject;
    Project PostgreSqlProject;
    Project GeneratorProject;
    Project[] TestProjects = [];

    bool IsTagBuild;
    
    public static int Main() => Execute<Build>(x => x.Compile);
    
    protected override void OnBuildInitialized()
    {
        base.OnBuildInitialized();
        ProjectModelTasks.Initialize();
        SqlServerProject = Solution.AdoGen_SqlServer;
        PostgreSqlProject = Solution.AdoGen_PostgreSql;
        GeneratorProject = Solution.AdoGen_Generator;
        TestProjects = Solution.AllProjects.Where(x => x.Name.EndsWith("Tests")).ToArray();
        IsTagBuild = GitRepo?.Branch?.StartsWith(TagPath) == true;
    }
    
    string ExtractReleaseNotes(string tagVersion)
    {
        if (!File.Exists(Changelog)) return string.Empty;

        // Accepts "## [2.1.0] - date", "## 2.1.0 - date" and the legacy "## Release 2.0.0.0 - date".
        // Versions are compared numerically, so "2.0.0" matches "2.0.0.0".
        var lines = File.ReadAllLines(Changelog);
        var sb = new StringBuilder();
        var capture = false;
        
        foreach (var line in lines)
        {
            var isHeading = line.StartsWith("## ", StringComparison.Ordinal);

            if (!capture)
            {
                capture = isHeading && VersionsMatch(ParseHeadingVersion(line), tagVersion);
                continue;
            }

            if (isHeading) break;
            
            sb.AppendLine(line);
        }
        
        return sb.ToString().Trim();
    }

    static string ParseHeadingVersion(string heading)
    {
        var text = heading[3..].Trim();
        if (text.StartsWith("Release ", StringComparison.OrdinalIgnoreCase)) text = text["Release ".Length..];
        text = text.TrimStart('[');

        var endIndex = text.IndexOfAny([']', ' ']);
        return endIndex < 0 ? text : text[..endIndex];
    }

    static bool VersionsMatch(string headingVersion, string tagVersion)
    {
        if (string.Equals(headingVersion, tagVersion, StringComparison.OrdinalIgnoreCase)) return true;

        var (headingNumeric, headingSuffix) = SplitPrerelease(headingVersion);
        var (tagNumeric, tagSuffix) = SplitPrerelease(tagVersion);

        return string.Equals(headingSuffix, tagSuffix, StringComparison.OrdinalIgnoreCase)
               && Version.TryParse(headingNumeric, out var a)
               && Version.TryParse(tagNumeric, out var b)
               && ToFourPart(a) == ToFourPart(b);
    }

    static (string Numeric, string Suffix) SplitPrerelease(string version)
    {
        var parts = version.Split('-', 2);
        return (parts[0], parts.Length > 1 ? parts[1] : "");
    }

    static Version ToFourPart(Version v) =>
        new(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0));

    static string ToNumericVersion(string version)
    {
        var (numeric, _) = SplitPrerelease(version);
        return Version.TryParse(numeric, out var parsed) ? ToFourPart(parsed).ToString() : "0.0.0.0";
    }
    
    string ResolveVersion()
    {
        if (!IsTagBuild) return LocalBuild;
        
        var tag = GitRepo!.Branch![TagPath.Length..];
        
        if (tag.StartsWith("v", StringComparison.OrdinalIgnoreCase)) tag = tag[1..];

        return tag != "" ? tag : LocalBuild;
    }
    
    Target CI => x => x
        .Description("Entry target for both local and CI builds")
        .DependsOn(PublishNuGet).OnlyWhenDynamic(() => IsServerBuild && IsTagBuild)
        .DependsOn(Test).OnlyWhenDynamic(() => !IsServerBuild || !IsTagBuild);
    
    Target Clean => x => x
        .Description("Deleting SourceDirectory obj and bin folders")
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/{obj,bin}").DeleteDirectories();
            OutputDirectory.CreateOrCleanDirectory();
        });

    Target Restore => x => x
        .Description("Restores the solution packages")
        .DependsOn(Clean)
        .Executes(() => DotNetRestore(x => x.SetProjectFile(Solution)));

    Target Compile => x => x
        .Description("Builds the solution")
        .DependsOn(Restore)
        .Executes(() =>
        {
            // Version properties must be applied at compile time: Pack runs with NoBuild, so properties passed
            // only to Pack would reach the .nuspec but not the assemblies.
            var version = ResolveVersion();
            var numericVersion = ToNumericVersion(version);

            DotNetBuild(x => x
                .SetProjectFile(Solution)
                .EnableNoRestore()
                .EnableNoLogo()
                .SetConfiguration(Configuration)
                .SetProperty("Version", version)
                .SetProperty("InformationalVersion", version)
                .SetProperty("AssemblyVersion", numericVersion)
                .SetProperty("FileVersion", numericVersion));
        });
    
    Target Test => x => x
        .Description("Runs all tests in the TestProject")
        .DependsOn(Compile)
        .Executes(() =>
        {
            foreach (var testProject in TestProjects)
            {
                DotNetTest(x =>
                    x.SetConfiguration(Configuration)
                        .SetNoBuild(true)
                        .SetProjectFile(testProject));    
            }
        });
    
    Target Pack => x => x
        .Description("Packs the provider and generator projects")
        .DependsOn(Test)
        .OnlyWhenDynamic(() => IsTagBuild || !IsServerBuild)
        .Executes(() =>
        {
            var version = ResolveVersion();
            var numericVersion = ToNumericVersion(version);
            var notes = ExtractReleaseNotes(version);

            Assert.True(!IsTagBuild || !string.IsNullOrWhiteSpace(notes),
                $"No release notes found in Changelog.md for version '{version}'. Add a '## [{version}] - <date>' section.");
            
            var providerProjects = new[] { SqlServerProject, PostgreSqlProject };

            foreach (var project in providerProjects)
            {
                DotNetPack(s => s
                    .SetProject(project)
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(OutputDirectory)
                    .SetNoBuild(true)
                    .EnableNoLogo()
                    .EnableNoRestore()
                    .EnableContinuousIntegrationBuild()
                    .SetProperty("Version", version)
                    .SetProperty("PackageVersion", version)
                    .SetProperty("InformationalVersion", version)
                    .SetProperty("AssemblyVersion", numericVersion)
                    .SetProperty("FileVersion", numericVersion)
                    .SetProperty("IncludeSymbols", "true")
                    .SetProperty("SymbolPackageFormat", "snupkg")
                    .SetProperty("PackageReleaseNotes", notes));
            }

            DotNetPack(s => s
                .SetProject(GeneratorProject)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(OutputDirectory)
                .EnableContinuousIntegrationBuild()
                .SetNoBuild(true)
                .EnableNoLogo()
                .EnableNoRestore()
                .SetProperty("Version", version)
                .SetProperty("PackageVersion", version)
                .SetProperty("InformationalVersion", version)
                .SetProperty("AssemblyVersion", numericVersion)
                .SetProperty("FileVersion", numericVersion)
                .SetProperty("IncludeSymbols", "false")
                .SetProperty("SymbolPackageFormat", "snupkg")
                .SetProperty("PackageReleaseNotes", notes));
        });
    
    Target PublishArtifacts => x => x
        .Description("Publishes the packed files to OutputDirectory")
        .DependsOn(Pack)
        .Produces(OutputDirectory / "*.nupkg")
        .Produces(OutputDirectory / "*.snupkg");
    
    Target PublishNuGet => x => x
        .DependsOn(PublishArtifacts)
        .OnlyWhenDynamic(() => IsServerBuild && IsTagBuild)
        .Requires(() => !string.IsNullOrWhiteSpace(NuGetApiKey))
        .Executes(() =>
        {
            var packages = OutputDirectory.GlobFiles("*.nupkg", "*.snupkg");

            foreach (var package in packages)
            {
                DotNetNuGetPush(s => s
                    .SetTargetPath(package)
                    .SetSource(NugetSource)
                    .SetApiKey(NuGetApiKey)
                    .EnableSkipDuplicate());
            }
        });
}

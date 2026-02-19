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
    
    Project AbstractionsProject;
    Project GeneratorProject;
    Project[] TestProjects = [];

    bool IsTagBuild;
    
    public static int Main() => Execute<Build>(x => x.Compile);
    
    protected override void OnBuildInitialized()
    {
        base.OnBuildInitialized();
        ProjectModelTasks.Initialize();
        AbstractionsProject = Solution.AdoGen_Abstractions;
        GeneratorProject = Solution.AdoGen_Generator;
        TestProjects = Solution.AllProjects.Where(x => x.Name.EndsWith("Tests")).ToArray();
        IsTagBuild = GitRepo?.Branch?.StartsWith(TagPath) == true;
    }
    
    string ExtractReleaseNotes(string tagVersion)
    {
        if (!File.Exists(Changelog)) return string.Empty;

        // Headings should look like: "## [0.1.0.0-alpha] - 2026-01-29"
        var lines = File.ReadAllLines(Changelog);
        var header = $"## [{tagVersion}]";
        var sb = new StringBuilder();
        var capture = false;
        
        foreach (var line in lines)
        {
            if (!capture)
            {
                capture = line.StartsWith(header, StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (line.StartsWith("## [")) break;
            
            sb.AppendLine(line);
        }
        
        return sb.ToString().Trim();
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
        .Executes(() => DotNetBuild(x => x.SetProjectFile(Solution).EnableNoRestore().EnableNoLogo().SetConfiguration(Configuration)));
    
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
        .Description("Packs the Abstraction and generator projects")
        .DependsOn(Test)
        .Executes(() =>
        {
            var version = ResolveVersion();
            var numericVersion = version.Split('-', 2)[0] + ".0";
            var notes = ExtractReleaseNotes(version);
            
            DotNetPack(s => s
                .SetProject(AbstractionsProject)
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
            var generator = OutputDirectory.GlobFiles("AdoGen.Generator*.nupkg").Single();
            var abstractions = OutputDirectory.GlobFiles("AdoGen.Abstractions*.nupkg").Single();
            var abstractionsSym = OutputDirectory.GlobFiles("AdoGen.Abstractions*.snupkg").Single();
            
            DotNetNuGetPush(s => s
                .SetTargetPath(generator)
                .SetSource(NugetSource)
                .SetApiKey(NuGetApiKey)
                .EnableSkipDuplicate());

            DotNetNuGetPush(s => s
                .SetTargetPath(abstractions)
                .SetSource(NugetSource)
                .SetApiKey(NuGetApiKey)
                .EnableSkipDuplicate());

            DotNetNuGetPush(s => s
                .SetTargetPath(abstractionsSym)
                .SetSource(NugetSource)
                .SetApiKey(NuGetApiKey)
                .EnableSkipDuplicate());
        });
}

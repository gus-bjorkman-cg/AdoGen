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
    On = [GitHubActionsTrigger.Push, GitHubActionsTrigger.PullRequest],
    InvokedTargets = [nameof(CI)], 
        CacheKeyFiles = ["**/global.json", "**/*.csproj", "**/Directory.Packages.props"])]
class Build : NukeBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);
    
    [Solution(GenerateProjects = true)]
    readonly Solution Solution;
    [GitRepository] 
    readonly GitRepository GitRepo;
    
    static AbsolutePath OutputDirectory => RootDirectory / "output";
    static AbsolutePath SourceDirectory => RootDirectory / "src";
    
    Project AbstractionsProject;
    Project GeneratorProject;
    Project TestProject;
    
    protected override void OnBuildInitialized()
    {
        base.OnBuildInitialized();
        ProjectModelTasks.Initialize();
        AbstractionsProject = Solution.AdoGen_Abstractions;
        GeneratorProject = Solution.AdoGen_Generator;
        TestProject = Solution.AdoGen_Tests;
    }

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Package version to produce. Default: 0.1.0.0")]
    readonly string PackageVersion = "0.1.0.0";
    
    bool IsMainBranch => GitRepo != null && GitRepo!.Branch!.Equals("refs/heads/main");
    
    Target CI => x => x
        .Description("Entry target for both local and CI builds")
        .DependsOn(PublishArtifacts).OnlyWhenDynamic(() => IsServerBuild && IsMainBranch)
        .DependsOn(Test).OnlyWhenDynamic(() => !IsServerBuild || !IsMainBranch);
    
    Target Clean => x => x
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/{obj,bin}").DeleteDirectories();
            OutputDirectory.CreateOrCleanDirectory();
        });

    Target Restore => x => x.DependsOn(Clean).Executes(() => DotNetRestore(x => x.SetProjectFile(Solution)));

    Target Compile => x => x
        .DependsOn(Restore)
        .Executes(() => DotNetBuild(x => x.SetProjectFile(Solution).EnableNoRestore().SetConfiguration(Configuration)));
    
    Target Test => x => x
        .DependsOn(Compile)
        .Executes(() => DotNetTest(x => 
            x.SetConfiguration(Configuration)
                .SetNoBuild(true)
                .SetProjectFile(TestProject)));
    
    Target Pack => x => x
        .DependsOn(Test)
        .Executes(() =>
        {
            DotNetPack(s => s
                .SetProject(AbstractionsProject)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(OutputDirectory)
                .SetNoBuild(true)
                .SetProperty("Version", PackageVersion)
                .SetProperty("PackageVersion", PackageVersion)
                .SetProperty("AssemblyVersion", PackageVersion)
                .SetProperty("FileVersion", PackageVersion)
            );

            DotNetPack(s => s
                .SetProject(GeneratorProject)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(OutputDirectory)
                .SetNoBuild(true)
                .SetProperty("Version", PackageVersion)
                .SetProperty("PackageVersion", PackageVersion)
                .SetProperty("AssemblyVersion", PackageVersion)
                .SetProperty("FileVersion", PackageVersion)
            );
        });
    
    Target PublishArtifacts => x => x
        .DependsOn(Pack)
        .Produces(OutputDirectory / "*.nupkg")
        .Produces(OutputDirectory / "*.snupkg");
}

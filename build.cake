//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
//////////////////////////////////////////////////////////////////////
var nugetVersion = string.Empty;
GitVersion gitVersionInfo;

Task("GetVersion")
    .Does(() =>
    {
        gitVersionInfo = GitVersion(new GitVersionSettings {
            OutputType = GitVersionOutput.Json
        });
        nugetVersion = gitVersionInfo.NuGetVersion;

        if(BuildSystem.IsRunningOnTeamCity)
            BuildSystem.TeamCity.SetBuildNumber(nugetVersion);

        Information($"Building dbup for Octopus v{0}", nugetVersion);
        Information("Informational Version {0}", gitVersionInfo.InformationalVersion);
        Verbose("GitVersion:\n{0}", gitVersionInfo);
    });

Task("Clean")
    .Does(() =>
    {
        CleanDirectories("./source/**/bin");
        CleanDirectories("./source/**/obj");
    });


Task("Build")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        DotNetCoreBuild("./source", new DotNetCoreBuildSettings
        {
            Configuration = configuration,
            ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}")
        });
    });

Task("PushPackage")
    .WithCriteria(BuildSystem.IsRunningOnTeamCity)
    .IsDependentOn("Build")
    .Does(() =>
    {
        NuGetPush($"./source/bin/{configuration}/octopus.dbup.sqlserver.{nugetVersion}.nupkg", new NuGetPushSettings {
            Source = "https://packages.octopushq.com/dependencies/nuget/index.json",
            ApiKey = EnvironmentVariable("FeedzIoApiKey")
        });
    });

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("Default")
    .IsDependentOn("PushPackage");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);

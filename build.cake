///////////////////////////////////////////////////////////////////////
// EXTENSIONS
///////////////////////////////////////////////////////////////////////

#addin nuget:?package=Cake.Docker&version=1.3.0

///////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////
// SETUP
///////////////////////////////////////////////////////////////////////

var solutionFile = "./DotnetClean.sln";

var srcDir = "./src";
var testDir = "./test";
var artifactsDir = "./artifacts";

var testProjects = GetFiles($"{testDir}/**/*.csproj");

///////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDir);
    DotNetClean(solutionFile);
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetRestore(solutionFile);
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    DotNetBuild(solutionFile, new DotNetBuildSettings
    {
        Configuration = configuration,
        NoRestore = true
    });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    Information("Running tests in parallel...");

    testProjects.AsParallel().ForAll(project =>
    {
        DotNetTest(project.FullPath, new DotNetTestSettings
        {
            Configuration = configuration,
            NoBuild = true,
            ResultsDirectory = artifactsDir,
            ArgumentCustomization = args => args.Append($"--logger \"trx;LogFileName={project.GetFilenameWithoutExtension()}_TestResults.trx\"")
        });
    });
});

Task("Local-Infra-Up")
    .Does(() =>
{
    var dockerComposeFile = "./infrastructure/docker-compose.yml";

    if (!FileExists(dockerComposeFile))
    {
        Error($"Docker Compose file not found at {dockerComposeFile}");
    }

    Information("Bringing up Docker Compose environment...");

    DockerComposeUp(new DockerComposeUpSettings
    {
        Files = new[] { dockerComposeFile }, 
        Detach = true,
        Build = true
    });
});

Task("Local-Infra-Down")
    .Does(() =>
{
    var dockerComposeFile = "./infrastructure/docker-compose.yml";

    if (!FileExists(dockerComposeFile))
    {
        Error($"Docker Compose file not found at {dockerComposeFile}");
    }

    Information("Tearing down Docker Compose environment...");

    DockerComposeDown(new DockerComposeDownSettings
    {
        Files = new[] { dockerComposeFile },
        RemoveOrphans = true,
        Volumes = true
    });
});

Task("Default")
    .IsDependentOn("Build");

///////////////////////////////////////////////////////////////////////
// RUN TARGET
///////////////////////////////////////////////////////////////////////

RunTarget(target);

// Load the recipe
#load nuget:?package=TestCentric.Cake.Recipe&version=1.1.0-dev00058
// Comment out above line and uncomment below for local tests of recipe changes
//#load ../TestCentric.Cake.Recipe/recipe/*.cake

BuildSettings.Initialize
(
	context: Context,
	title: "TestCentric Agent Core",
	solutionFile: "TestCentric.Agent.Core.sln",
	unitTests: "**/*.tests.exe",
	githubOwner: "TestCentric",
	githubRepository: "TestCentric.Agent.Core"
);

BuildSettings.Packages.Add(new NuGetPackage(
	"TestCentric.Agent.Core",
	title: "TestCentric Agent Core",
	source: "nuget/TestCentric.Agent.Core.nuspec",
	checks: new PackageCheck[] {
		HasFiles("LICENSE.txt", "README.md", "testcentric.png"),
		HasDirectory("lib/net70").WithFiles("TestCentric.Agent.Core.dll"),
		HasDependency("TestCentric.InternalTrace"),
		HasDependency("TestCentric.Engine.Core")
	}
	/*packageContent: new PackageContent()
		.WithRootFiles("../../LICENSE.txt", "../../README.md", "../../testcentric.png")
		.WithDirectories(
			new DirectoryContent("lib/net70").WithFiles("agent/TestCentric.Agent.Core.dll") )
		.WithDependencies(
			new PackageReference("TestCentric.InternalTrace", "1.0.0"),
			new PackageReference("TestCentric.Engine.Core", "2.0.0-beta3") )*/
));

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Appveyor")
	.IsDependentOn("DumpSettings")
	.IsDependentOn("Build")
	.IsDependentOn("Test")
	.IsDependentOn("Package")
	.IsDependentOn("Publish")
	.IsDependentOn("CreateDraftRelease")
	.IsDependentOn("CreateProductionRelease");

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(CommandLineOptions.Target);

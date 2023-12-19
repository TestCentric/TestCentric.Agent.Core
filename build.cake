// Load the recipe
#load nuget:?package=TestCentric.Cake.Recipe&version=1.1.0-dev00064
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
		HasDirectory("lib/net20").WithFiles("TestCentric.Agent.Core.dll"),
		HasDirectory("lib/net462").WithFiles("TestCentric.Agent.Core.dll"),
		HasDirectory("lib/netstandard2.0").WithFiles("TestCentric.Agent.Core.dll"),
		HasDirectory("lib/netcoreapp3.1").WithFiles("TestCentric.Agent.Core.dll"),
		HasDirectory("lib/net6.0").WithFiles("TestCentric.Agent.Core.dll"),
		HasDirectory("lib/net8.0").WithFiles("TestCentric.Agent.Core.dll"),
		HasDependency(PackageReference.EngineApi.LatestDevBuild)
			.WithFiles("lib/net20/TestCentric.Engine.Api.dll",
					   "lib/net462/TestCentric.Engine.Api.dll",
					   "lib/netstandard2.0/TestCentric.Engine.Api.dll"),
		HasDependency(PackageReference.Extensibility)
			.WithFiles("lib/net20/TestCentric.Extensibility.dll",
					   "lib/net20/TestCentric.Extensibility.Api.dll",
					   "lib/net462/TestCentric.Extensibility.dll",
					   "lib/net462/TestCentric.Extensibility.Api.dll",
					   "lib/netstandard2.0/TestCentric.Extensibility.dll",
					   "lib/netstandard2.0/TestCentric.Extensibility.Api.dll"),
		HasDependency(PackageReference.Metadata)
			.WithFiles("lib/net20/testcentric.engine.metadata.dll",
					   "lib/net40/testcentric.engine.metadata.dll",
					   "lib/netstandard2.0/testcentric.engine.metadata.dll"),
		HasDependency(PackageReference.InternalTrace)
			.WithFiles("lib/net20/TestCentric.InternalTrace.dll",
					   "lib/net462/TestCentric.InternalTrace.dll",
					   "lib/netstandard2.0/TestCentric.InternalTrace.dll"),
	}
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

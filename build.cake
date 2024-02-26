// Load the recipe
#load nuget:?package=TestCentric.Cake.Recipe&version=1.1.1
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

var mockAssemblyExpectedResult = new ExpectedResult("Failed")
    {
        Total = 37,
        Passed = 23,
        Failed = 5,
        Warnings = 1,
        Inconclusive = 1,
        Skipped = 7,
		Assemblies = new ExpectedAssemblyResult[] {
			new ExpectedAssemblyResult("mock-assembly.dll")}
    };

var packageTests = new List<PackageTest>();

// .NET Core tests

packageTests.Add(new PackageTest(1, "Net35Test", "Run mock-assembly.dll targeting .NET 3.5",
    "mock-assembly/net35/mock-assembly.dll",
    mockAssemblyExpectedResult));

packageTests.Add(new PackageTest(1, "Net462Test", "Run mock-assembly.dll targeting .NET 4.6.2",
    "mock-assembly/net462/mock-assembly.dll",
    mockAssemblyExpectedResult));

packageTests.Add(new PackageTest(1, "NetCore31Test", "Run mock-assembly.dll targeting .NET Core 3.1",
    "mock-assembly/netcoreapp3.1/mock-assembly.dll",
    mockAssemblyExpectedResult));

packageTests.Add(new PackageTest(1, "Net60Test", "Run mock-assembly.dll targeting .NET 6.0",
    "mock-assembly/net6.0/mock-assembly.dll",
    mockAssemblyExpectedResult));

packageTests.Add(new PackageTest(1, "Net80Test", "Run mock-assembly.dll targeting .NET 8.0",
    "mock-assembly/net8.0/mock-assembly.dll",
    mockAssemblyExpectedResult));

// Asp .NET Core Tests

packageTests.Add(new PackageTest(1, "AspNetCore31Test", "Run test using AspNetCore under .NET Core 3.1",
    "aspnetcore-test/netcoreapp3.1/aspnetcore-test.dll",
    new ExpectedResult("Passed"){ Assemblies = new ExpectedAssemblyResult[] {
		new ExpectedAssemblyResult("aspnetcore-test.dll")} }));

packageTests.Add(new PackageTest(1, "AspNetCore50Test", "Run test using AspNetCore under .NET 5.0",
    "aspnetcore-test/net5.0/aspnetcore-test.dll",
    new ExpectedResult("Passed"){ Assemblies = new ExpectedAssemblyResult[] {
		new ExpectedAssemblyResult("aspnetcore-test.dll")} }));

packageTests.Add(new PackageTest(1, "AspNetCore60Test", "Run test using AspNetCore under .NET 6.0",
    "aspnetcore-test/net6.0/aspnetcore-test.dll",
    new ExpectedResult("Passed"){ Assemblies = new ExpectedAssemblyResult[] {
		new ExpectedAssemblyResult("aspnetcore-test.dll")} }));

packageTests.Add(new PackageTest(1, "AspNetCore70Test", "Run test using AspNetCore under .NET 7.0",
    "aspnetcore-test/net7.0/aspnetcore-test.dll",
    new ExpectedResult("Passed"){ Assemblies = new ExpectedAssemblyResult[] {
		new ExpectedAssemblyResult("aspnetcore-test.dll")} }));

packageTests.Add(new PackageTest(1, "AspNetCore80Test", "Run test using AspNetCore under .NET 8.0",
    "aspnetcore-test/net8.0/aspnetcore-test.dll",
    new ExpectedResult("Passed"){ Assemblies = new ExpectedAssemblyResult[] {
		new ExpectedAssemblyResult("aspnetcore-test.dll")} }));

// Windows Forms Tests

packageTests.Add(new PackageTest(1, "WindowsFormsNet50Test", "Run test using windows forms under .NET 5.0",
    "windows-forms-test/net5.0-windows/windows-forms-test.dll",
    new ExpectedResult("Passed")
    {
        Assemblies = new [] { new ExpectedAssemblyResult("windows-forms-test.dll") }
    }));

packageTests.Add(new PackageTest(1, "WindowsFormsNet60Test", "Run test using windows forms under .NET 6.0",
    "windows-forms-test/net6.0-windows/windows-forms-test.dll",
    new ExpectedResult("Passed")
    {
        Assemblies = new [] { new ExpectedAssemblyResult("windows-forms-test.dll") }
    }));

packageTests.Add(new PackageTest(1, "WindowsFormsNet70Test", "Run test using windows forms under .NET 7.0",
    "windows-forms-test/net7.0-windows/windows-forms-test.dll",
    new ExpectedResult("Passed")
    {
        Assemblies = new [] { new ExpectedAssemblyResult("windows-forms-test.dll") }
    }));

packageTests.Add(new PackageTest(1, "WindowsFormsNet80Test", "Run test using windows forms under .NET 8.0",
    "windows-forms-test/net8.0-windows/windows-forms-test.dll",
    new ExpectedResult("Passed")
    {
        Assemblies = new [] { new ExpectedAssemblyResult("windows-forms-test.dll") }
    }));

// Define the package

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
		HasDependency("TestCentric.InternalTrace", "1.2.0")
			.WithFiles("lib/net20/TestCentric.InternalTrace.dll",
					   "lib/net462/TestCentric.InternalTrace.dll",
					   "lib/netstandard2.0/TestCentric.InternalTrace.dll"),
	},
	testRunner: new DirectTestAgentRunner(),
	tests: packageTests
));

//////////////////////////////////////////////////////////////////////
// TEST BED RUNNER
//////////////////////////////////////////////////////////////////////

public class DirectTestAgentRunner : TestRunner
{
	public override int Run(string arguments)
	{
		// First argument must be relative path to a test assembly.
		// It's immediate directory name is the name of the runtime.
		string testAssembly = arguments.Trim();
		testAssembly = BuildSettings.OutputDirectory + (testAssembly[0] == '"'
			? testAssembly.Substring(1, testAssembly.IndexOf('"', 1) - 1)
			: testAssembly.Substring(0, testAssembly.IndexOf(' ')));

		if (!System.IO.File.Exists(testAssembly))
			throw new FileNotFoundException($"File not found: {testAssembly}");

		string testRuntime = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(testAssembly));
		string agentRuntime = testRuntime;

		if (agentRuntime.EndsWith("-windows"))
			agentRuntime = agentRuntime.Substring(0, 6);

		// Avoid builds we don't have
		if (agentRuntime == "net35")
			agentRuntime = "net20";
		else if (agentRuntime == "net5.0")
			agentRuntime = "net6.0";

		ExecutablePath = BuildSettings.OutputDirectory + $"direct-test-agent/{agentRuntime}/DirectTestAgent.exe";

		if (!System.IO.File.Exists(ExecutablePath))
			throw new FileNotFoundException($"File not found: {ExecutablePath}");

        Console.WriteLine($"Trying to run {ExecutablePath} with arguments {arguments}");

		return BuildSettings.Context.StartProcess(ExecutablePath, new ProcessSettings()
		{
			Arguments = arguments,
			WorkingDirectory = BuildSettings.OutputDirectory
		});
	}
}

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

Build.Run();

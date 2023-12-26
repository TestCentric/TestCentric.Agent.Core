// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TestCentric.Engine.Internal;
using TestCentric.Tests.Assemblies;

namespace TestCentric.Agents
{
    public class AgentDirectRunnerTests
    {
        [Test]
        public void RunAgentDirectly()
        {
            RunTestUnderTestBed(typeof(MockAssembly).Assembly.Location);
        }

        private void RunTestUnderTestBed(string testAssembly)
        {
            string agentAssembly = typeof(DirectTestAgent).Assembly.Location;

#if NETFRAMEWORK
            string agentExe = agentAssembly;
#else
            string agentExe = Path.ChangeExtension(agentAssembly, ".exe");
#endif

            var startInfo = new ProcessStartInfo(agentExe);
            startInfo.Arguments = testAssembly;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;

            var process = Process.Start(startInfo);
            process.WaitForExit();

            string output = process.StandardOutput.ReadToEnd();
            int index = output.IndexOf("Test Run Summary");
            if (index > 0)
                output = output.Substring(index);

            Console.WriteLine(output);
            Console.WriteLine($"Agent process exited with rc={process.ExitCode}");

            if (index < 0)
                Assert.Fail("No Summary Report found");
        }
    }
}

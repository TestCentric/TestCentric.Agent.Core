// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TestCentric.Tests.Assemblies;

namespace TestCentric.Agents
{
    public class AgentDirectRunnerTests
    {
        [Test, Ignore("Exits NUnitLite - must run this in a separate process somehow")]
        public void RunAgentDirectly()
        {
            var options = new AgentOptions(MockAssembly.AssemblyPath);
            var runner = new AgentDirectRunner(options);
            runner.ExecuteTestsDirectly();
        }
    }
}

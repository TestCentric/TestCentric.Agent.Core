// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if NETCOREAPP3_1_OR_GREATER
using NUnit.Framework;
using System;
using System.Linq;

namespace TestCentric.Engine.Internal
{
    public static class DotNetRuntimesTests
    {
        [TestCaseSource(typeof(DotNetRuntimes), nameof(DotNetRuntimes.InstalledRuntimes))]
        public static void AllRuntimeDirectoriesExist(RuntimeInfo runtime)
        {
            Assert.That(System.IO.Directory.Exists(runtime.Location));
        }
    }
}
#endif

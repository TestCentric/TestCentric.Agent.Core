// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if NETCOREAPP3_1_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestCentric.Engine.Internal
{
    public static class DotNetRuntimes
    {
        static Logger log = InternalTrace.GetLogger("DotNetRuntimes");

        static DotNetRuntimes()
        {
            System.Diagnostics.Process process;
            InstalledRuntimes = new RuntimeInfoCollection();

            var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet");
            startInfo.Arguments = "--list-runtimes";
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;

            try
            {
                process = System.Diagnostics.Process.Start(startInfo);
                process.WaitForExit();
            }
            catch
            {
                // Dotnet is not installed, collection will be empty
                log.Error("DotNet does not appear to be installed");
                return;
            }

            while (process.StandardOutput.Peek() > 0)
            {
                string line = process.StandardOutput.ReadLine();
                var runtimeInfo = RuntimeInfo.FromText(line);
                InstalledRuntimes.Add(runtimeInfo);
                //log.Debug($"Found {runtimeInfo.Name} {runtimeInfo.Version}");
            }
        }

        public static RuntimeInfoCollection InstalledRuntimes { get; }

        public static RuntimeInfo GetBestRuntime(string name, Version version)
        {
            return InstalledRuntimes.GetBestRuntime(name, version);
        }
    }
}
#endif

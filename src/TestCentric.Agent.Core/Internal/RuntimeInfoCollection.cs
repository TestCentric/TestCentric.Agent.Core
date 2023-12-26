// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if NETCOREAPP3_1_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TestCentric.Engine.Internal;

namespace TestCentric.Engine.Internal
{
    public class RuntimeInfoCollection : List<RuntimeInfo>
    {
        static Logger log = InternalTrace.GetLogger("RuntimeInfoCollection");

        /// <summary>
        /// Construct an empty RuntimeInfoCollection - used for testing
        /// </summary>
        public RuntimeInfoCollection() { }

        public IEnumerable<RuntimeInfo> GetRuntimes(string runtimeName)
        {
            return this.Where(rt => rt.Name == runtimeName);
        }

        /// <summary>
        /// Get the best runtime for a given target. The returned version must
        /// be greater than than or equal to the target version. We handle
        /// targets with two components (Major+Minor) differently from those
        /// with three (Major+Minor+Build).
        /// </summary>
        public RuntimeInfo GetBestRuntime(string runtimeName, Version version)
        {
            //log.Info($"Target: {runtimeName} version {version}");

            var candidates = GetRuntimes(runtimeName);
            RuntimeInfo best = null;

            foreach (var rt in candidates)
            {
                // Skip entries, which can't meet our criteria
                if (rt.Version.Major < version.Major ||
                    rt.Version.Major == version.Major && rt.Version.Minor < version.Minor ||
                    rt.Version.Major == version.Major && rt.Version.Minor == version.Minor && rt.Version.Build < version.Build)
                {
                    //log.Debug($"Skipping version {rt.Version}");
                    continue;
                }

                // At this point, the runtime meets our criteria, so we check best found so far.
                if (best == null ||
                    best.Version.Major > rt.Version.Major ||
                    best.Version.Major == rt.Version.Major && best.Version.Minor > rt.Version.Minor ||
                    best.Version.Major == rt.Version.Major && best.Version.Minor == rt.Version.Minor && best.Version.Build > rt.Version.Build)
                {
                    best = rt;
                }
            }

            return best;
        }
    }
}
#endif

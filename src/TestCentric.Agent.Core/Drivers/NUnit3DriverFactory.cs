﻿// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.Reflection;
using TestCentric.Engine.Extensibility;

namespace TestCentric.Engine.Drivers
{
    public class NUnit3DriverFactory : IDriverFactory
    {
        private const string NUNIT_FRAMEWORK = "nunit.framework";
        static Logger log = InternalTrace.GetLogger(typeof(NUnit3DriverFactory));

        /// <summary>
        /// Gets a flag indicating whether a given assembly name and version
        /// represent a test framework supported by this factory.
        /// </summary>
        /// <param name="reference">An AssemblyName referring to the possible test framework.</param>
        public bool IsSupportedTestFramework(AssemblyName reference)
        {
            // The driver actually supports version 4 as well as 3 of NUnit
            return NUNIT_FRAMEWORK.Equals(reference.Name, StringComparison.OrdinalIgnoreCase) && reference.Version.Major >= 3;
        }

#if NETFRAMEWORK
        /// <summary>
        /// Gets a driver for a given test assembly and a framework
        /// which the assembly is already known to reference.
        /// </summary>
        /// <param name="domain">The domain in which the assembly will be loaded</param>
        /// <param name="reference">An AssemblyName referring to the test framework.</param>
        /// <returns></returns>
        // TODO: Remove this overload after engine api is updated
        public IFrameworkDriver GetDriver(AppDomain domain, AssemblyName reference)
        {
            Guard.ArgumentValid(IsSupportedTestFramework(reference), "Invalid framework", "reference");

            return new NUnit3FrameworkDriver(domain, reference);
        }
#else
        /// <summary>
        /// Gets a driver for a given test assembly and a framework
        /// which the assembly is already known to reference.
        /// </summary>
        /// <param name="domain">The domain in which the assembly will be loaded</param>
        /// <param name="reference">An AssemblyName referring to the test framework.</param>
        /// <returns></returns>
        public IFrameworkDriver GetDriver(AssemblyName reference)
        {
            Guard.ArgumentValid(IsSupportedTestFramework(reference), "Invalid framework", "reference");
#if NETSTANDARD
            log.Info("Using NUnitNetStandardDriver");
            return new NUnitNetStandardDriver();
#else
            log.Info("Using NUnitNetCore31Driver");
            return new NUnitNetCore31Driver();
#endif
        }
#endif
        }
}

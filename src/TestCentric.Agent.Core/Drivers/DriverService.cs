﻿// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TestCentric.Metadata;
using TestCentric.Engine.Internal;
using TestCentric.Engine.Extensibility;
using TestCentric.Extensibility;

namespace TestCentric.Engine.Drivers
{
    /// <summary>
    /// The DriverService provides drivers able to load and run tests
    /// using various frameworks.
    /// </summary>
    public class DriverService : IDriverService
    {
        static readonly Logger log = InternalTrace.GetLogger("DriverService");

        readonly IList<IDriverFactory> _factories = new List<IDriverFactory>();

        public DriverService()
        {
            log.Debug("Creating ExtensionManager");
            var thisAssembly = Assembly.GetExecutingAssembly();
            var extensionManager = new ExtensionManager("/TestCentric/Engine/TypeExtensions/");

            foreach (IDriverFactory factory in extensionManager.GetExtensions<IDriverFactory>())
                _factories.Add(factory);

#if NETFRAMEWORK
            var node = extensionManager.GetExtensionNode("/NUnit/Engine/NUnitV2Driver");
            if (node != null)
                _factories.Add(new NUnit2DriverFactory(node));
#endif

            _factories.Add(new NUnit3DriverFactory());
        }

        /// <summary>
        /// Get a driver suitable for use with a particular test assembly.
        /// </summary>
        /// <param name="domain">The application domain to use for the tests</param>
        /// <param name="assemblyPath">The full path to the test assembly</param>
        /// <param name="targetFramework">The value of any TargetFrameworkAttribute on the assembly, or null</param>
        /// <param name="skipNonTestAssemblies">True if non-test assemblies should simply be skipped rather than reporting an error</param>
        /// <returns></returns>
        public IFrameworkDriver GetDriver(AppDomain domain, string assemblyPath, string targetFramework, bool skipNonTestAssemblies)
        {
            if (!File.Exists(assemblyPath))
                return new InvalidAssemblyFrameworkDriver(assemblyPath, "File not found: " + assemblyPath);

            if (!PathUtils.IsAssemblyFileType(assemblyPath))
                return new InvalidAssemblyFrameworkDriver(assemblyPath, "File type is not supported");

            if (targetFramework != null)
            {
                // This takes care of an issue with Roslyn. It may get fixed, but we still
                // have to deal with assemblies having this setting. I'm assuming that
                // any true Portable assembly would have a Profile as part of its name.
                var platform = targetFramework == ".NETPortable,Version=v5.0"
                    ? ".NETStandard"
                    : targetFramework.Split(new char[] { ',' })[0];

                if (platform == "Silverlight" || platform == ".NETPortable" || platform == ".NETStandard" || platform == ".NETCompactFramework")
                    return new InvalidAssemblyFrameworkDriver(assemblyPath, platform + " test assemblies are not supported by this version of the engine");
            }

            try
            {
                using (var assemblyDef = AssemblyDefinition.ReadAssembly(assemblyPath))
                {
                    if (skipNonTestAssemblies)
                    {
                        foreach (var attr in assemblyDef.CustomAttributes)
                            if (attr.AttributeType.FullName == "NUnit.Framework.NonTestAssemblyAttribute")
                                return new SkippedAssemblyFrameworkDriver(assemblyPath);
                    }

                    var references = new List<AssemblyName>();
                    foreach (var cecilRef in assemblyDef.MainModule.AssemblyReferences)
                        references.Add(new AssemblyName(cecilRef.FullName));

                    foreach (var factory in _factories)
                    {
                        log.Debug($"Trying {factory.GetType().Name}");

                        foreach (var reference in references)
                        {
                            if (factory.IsSupportedTestFramework(reference))
#if NETFRAMEWORK
                            return factory.GetDriver(domain, reference);
#else
                            return factory.GetDriver(reference);
#endif
                        }
                    }
                }
            }
            catch (BadImageFormatException ex)
            {
                return new InvalidAssemblyFrameworkDriver(assemblyPath, ex.Message);
            }

            if (skipNonTestAssemblies)
                return new SkippedAssemblyFrameworkDriver(assemblyPath);
            else
                return new InvalidAssemblyFrameworkDriver(assemblyPath, string.Format("No suitable tests found in '{0}'.\n" +
                                                                              "Either assembly contains no tests or proper test driver has not been found.", assemblyPath));
        }
    }
}

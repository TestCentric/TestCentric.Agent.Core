// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if NETCOREAPP3_1_OR_GREATER

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace TestCentric.Engine.Internal
{
    internal sealed class TestAssemblyResolver : IDisposable
    {
        private static readonly Logger log = InternalTrace.GetLogger(nameof(TestAssemblyResolver));

        // The AssemblyLoadContext created for the current TestAssembly
        public TestAssemblyLoadContext LoadContext { get; }

        // The path to the current TestAssembly
        public string TestAssemblyPath { get; }

        // Our ResolverStrategies
        public List<ResolutionStrategy> ResolverStrategies { get; }

        public TestAssemblyResolver(TestAssemblyLoadContext loadContext, string assemblyPath)
        {
            LoadContext = loadContext;
            TestAssemblyPath = assemblyPath;

            ResolverStrategies = new List<ResolutionStrategy>
            {
                new TrustedPlatformResolutionStrategy(this),
                new RuntimeLibraryResolutionStrategy(this),
                new AspNetCoreResolutionStrategy(this),
                new WindowsDesktopResolutionStrategy(this)
            };

            LoadContext.Resolving += OnResolving;
        }

        public void Dispose()
        {
            LoadContext.Resolving -= OnResolving;
        }

        private Assembly OnResolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            log.Info($"Resolving {assemblyName}");

            for (int index = 0; index < ResolverStrategies.Count; index++)
            {
                var strategy = ResolverStrategies[index];

                if (strategy.TryLoadAssembly(context, assemblyName, out var loadedAssembly))
                {
                    if (index > 0)
                    {
                        // Simplistic approach to favoring the strategy that succeeds
                        // by moving it to the start of the list. This is safe because
                        // we are about to return from the method, exiting the loop.
                        ResolverStrategies.RemoveAt(index);
                        ResolverStrategies.Insert(0, strategy);
                    }

                    return loadedAssembly;
                }
            }

            return null;
        }
    }
}
#endif

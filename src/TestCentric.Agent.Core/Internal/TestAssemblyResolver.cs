// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if NETCOREAPP3_1_OR_GREATER

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace TestCentric.Engine.Internal
{
    internal sealed class TestAssemblyResolver : IDisposable
    {
        private static readonly Logger log = InternalTrace.GetLogger(nameof(TestAssemblyResolver));

        private readonly ICompilationAssemblyResolver _assemblyResolver;
        private readonly DependencyContext _dependencyContext;
        private readonly AssemblyLoadContext _loadContext;

        //private static readonly string NET_CORE_RUNTIME = "Microsoft.NETCore.App";
        private static readonly string WINDOWS_DESKTOP_RUNTIME = "Microsoft.WindowsDesktop.App";
        private static readonly string ASP_NET_CORE_RUNTIME = "Microsoft.AspNetCore.App";
        
        private static readonly string[] AdditionalRuntimes = new [] {
            ASP_NET_CORE_RUNTIME, WINDOWS_DESKTOP_RUNTIME
        };

        public TestAssemblyResolver(AssemblyLoadContext loadContext, string assemblyPath)
        {
            _loadContext = loadContext;
            _dependencyContext = DependencyContext.Load(loadContext.LoadFromAssemblyPath(assemblyPath));

            _assemblyResolver = new CompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
            {
                new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(assemblyPath)),
                new ReferenceAssemblyPathResolver(),
                new PackageCompilationAssemblyResolver()
            });

            _loadContext.Resolving += OnResolving;
        }

        public void Dispose()
        {
            _loadContext.Resolving -= OnResolving;
        }

        private Assembly OnResolving(AssemblyLoadContext context, AssemblyName name)
        {
            log.Info($"Resolving {name}");

            if (TryLoadFromTrustedPlatformAssemblies(context, name, out var loadedAssembly))
            {
                log.Info($"  TrustedPlatformAssemblies: {loadedAssembly.Location}");

                return loadedAssembly;
            }

            foreach (var library in _dependencyContext.RuntimeLibraries)
            {
                var wrapper = new CompilationLibrary(
                    library.Type,
                    library.Name,
                    library.Version,
                    library.Hash,
                    library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
                    library.Dependencies,
                    library.Serviceable);

                var assemblies = new List<string>();
                _assemblyResolver.TryResolveAssemblyPaths(wrapper, assemblies);

                foreach (var assemblyPath in assemblies)
                {
                    if (name.Name == Path.GetFileNameWithoutExtension(assemblyPath))
                    {
                        loadedAssembly = _loadContext.LoadFromAssemblyPath(assemblyPath);
                        log.Info($"  Runtime Library {library.Name}: {loadedAssembly.Location}");

                        return loadedAssembly;
                    }
                }
            }

            if (name.Version == null)
                return null;

            foreach (string runtime in AdditionalRuntimes)
            {
                var runtimeDir = DotNetRuntimes.GetBestRuntime(runtime, name.Version).Location;
                if (runtimeDir != null)
                {
                    string candidate = Path.Combine(runtimeDir, name.Name + ".dll");
                    if (File.Exists(candidate))
                    {
                        log.Info($"  Runtime {runtime}: {candidate}");
                        return _loadContext.LoadFromAssemblyPath(candidate);
                    }
                }
            }

            return null;
        }

        private static bool TryLoadFromTrustedPlatformAssemblies(AssemblyLoadContext context, AssemblyName assemblyName, out Assembly loadedAssembly)
        {
            // https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/default-probing
            loadedAssembly = null;
            var trustedAssemblies = System.AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            if (string.IsNullOrEmpty(trustedAssemblies))
            {
                return false;
            }

            //log.Debug($"Trusted Platform Assemblies: {trustedAssemblies}");
            var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ";" : ":";
            foreach (var assemblyPath in trustedAssemblies.Split(separator))
            {
                var fileName = Path.GetFileNameWithoutExtension(assemblyPath);
                if (string.Equals(fileName, assemblyName.Name, StringComparison.InvariantCultureIgnoreCase) == false)
                {
                    continue;
                }

                if (File.Exists(assemblyPath))
                {
                    loadedAssembly = context.LoadFromAssemblyPath(assemblyPath);
                    return true;
                }
            }

            return false;
        }
    }
}
#endif

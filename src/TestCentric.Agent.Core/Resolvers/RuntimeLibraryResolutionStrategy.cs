// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if NETCOREAPP3_1_OR_GREATER

using Microsoft.Extensions.DependencyModel.Resolution;
using Microsoft.Extensions.DependencyModel;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Linq;

namespace TestCentric.Engine.Internal
{
    internal class RuntimeLibraryResolutionStrategy : ResolutionStrategy
    {
        private static readonly Logger log = InternalTrace.GetLogger(nameof(RuntimeLibraryResolutionStrategy));

        private List<CompilationLibrary> _libraries = new List<CompilationLibrary>();

        public RuntimeLibraryResolutionStrategy(TestAssemblyResolver resolver) : base(resolver)
        {
            var dependencyContext = DependencyContext.Load(LoadContext.LoadFromAssemblyPath(TestAssemblyPath));
            if (dependencyContext != null)
                foreach (var library in dependencyContext.RuntimeLibraries)
                    _libraries.Add(
                        new CompilationLibrary(
                            library.Type,
                            library.Name,
                            library.Version,
                            library.Hash,
                            library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
                            library.Dependencies,
                            library.Serviceable));
        }

        public override Assembly TryLoadAssembly(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            foreach (var library in _libraries)
            {
                var assemblies = new List<string>();
                var assemblyResolver = new CompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
                {
                        new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(TestAssemblyPath)),
                        new ReferenceAssemblyPathResolver(),
                        new PackageCompilationAssemblyResolver()
                });

                assemblyResolver.TryResolveAssemblyPaths(library, assemblies);

                foreach (var assemblyPath in assemblies)
                {
                    if (assemblyName.Name == Path.GetFileNameWithoutExtension(assemblyPath))
                    {
                        log.Debug($"Resolved to {assemblyPath}");
                        return LoadContext.LoadFromAssemblyPath(assemblyPath);
                    }
                }
            }

            log.Debug("Failed!");
            return null;
        }
    }
}
#endif

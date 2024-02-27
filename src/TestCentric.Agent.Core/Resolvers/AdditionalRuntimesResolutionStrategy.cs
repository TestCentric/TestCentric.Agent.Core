// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if NETCOREAPP3_1_OR_GREATER

using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace TestCentric.Engine.Internal
{
    internal abstract class AdditionalRuntimesResolutionStrategy : ResolutionStrategy
    {
        private static readonly Logger log = InternalTrace.GetLogger(nameof(AdditionalRuntimesResolutionStrategy));

        protected abstract string RuntimeName { get; }

        public AdditionalRuntimesResolutionStrategy(TestAssemblyResolver resolver) : base(resolver) { }

        public override Assembly TryLoadAssembly(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            // This strategy requires a version, which may not be present
            if (assemblyName.Version == null)
                return null;

            var runtimeDir = DotNetRuntimes.GetBestRuntime(RuntimeName, assemblyName.Version).Location;
            if (runtimeDir != null)
            {
                string candidate = Path.Combine(runtimeDir, assemblyName.Name + ".dll");
                if (File.Exists(candidate))
                    return LoadContext.LoadFromAssemblyPath(candidate);
            }

            return null;
        } 
    }

    internal class AspNetCoreResolutionStrategy : AdditionalRuntimesResolutionStrategy
    {
        public AspNetCoreResolutionStrategy(TestAssemblyResolver resolver) : base(resolver) { }

        protected override string RuntimeName => "Microsoft.AspNetCore.App";
    }

    internal class WindowsDesktopResolutionStrategy : AdditionalRuntimesResolutionStrategy
    {
        public WindowsDesktopResolutionStrategy(TestAssemblyResolver resolver) : base(resolver) { }
        
        protected override string RuntimeName => "Microsoft.WindowsDesktop.App";
    }
}
#endif

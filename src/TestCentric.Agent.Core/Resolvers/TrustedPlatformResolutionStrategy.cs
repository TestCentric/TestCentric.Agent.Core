// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if NETCOREAPP3_1_OR_GREATER

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace TestCentric.Engine.Internal
{
    internal class TrustedPlatformResolutionStrategy : ResolutionStrategy
    {
        private static readonly Logger log = InternalTrace.GetLogger(nameof(TrustedPlatformResolutionStrategy));

        private readonly string[] _trustedAssemblies = new string[0];

        public TrustedPlatformResolutionStrategy(TestAssemblyResolver resolver) : base(resolver)
        {
            // https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/default-probing
            var data = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;

            if (data != null)
            {
                var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ";" : ":";
                _trustedAssemblies = data.Split(separator);
            }

        }

        public override Assembly TryLoadAssembly(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            foreach (var assemblyPath in _trustedAssemblies)
            {
                var fileName = Path.GetFileNameWithoutExtension(assemblyPath);
                if (string.Equals(fileName, assemblyName.Name, StringComparison.InvariantCultureIgnoreCase) && File.Exists(assemblyPath))
                {
                    log.Debug($"Resolved to {assemblyPath}");
                    return context.LoadFromAssemblyPath(assemblyPath);
                }
            }

            log.Debug("Failed!");
            return null;
        }
    }
}
#endif

// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if NETCOREAPP3_1_OR_GREATER

using System.Reflection;
using System.Runtime.Loader;
using TestCentric.Engine.Internal;

namespace TestCentric.Engine.Internal
{
    public abstract class ResolutionStrategy
    {
        internal TestAssemblyLoadContext LoadContext { get; }
        internal string TestAssemblyPath { get; }


        internal ResolutionStrategy(TestAssemblyResolver resolver)
        {
            LoadContext = resolver.LoadContext;
            TestAssemblyPath = resolver.TestAssemblyPath;
        }

        public bool TryLoadAssembly(AssemblyLoadContext context, AssemblyName assemblyName, out Assembly loadedAssembly)
        {
            loadedAssembly = TryLoadAssembly(context, assemblyName);
            return loadedAssembly != null;
        }

        public abstract Assembly TryLoadAssembly(AssemblyLoadContext context, AssemblyName assemblyName);
    }
}
#endif

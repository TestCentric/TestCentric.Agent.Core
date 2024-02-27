// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if NETCOREAPP3_1_OR_GREATER

using System;
using System.Reflection;
using System.Runtime.Loader;
using TestCentric.Engine.Internal;

namespace TestCentric.Engine.Internal
{
    public abstract class ResolutionStrategy
    {
        private int _totalCalls;

        internal TestAssemblyLoadContext LoadContext { get; }
        internal string TestAssemblyPath { get; }


        internal ResolutionStrategy(TestAssemblyResolver resolver)
        {
            LoadContext = resolver.LoadContext;
            TestAssemblyPath = resolver.TestAssemblyPath;
        }

        public bool TryLoadAssembly(AssemblyLoadContext context, AssemblyName assemblyName, out Assembly loadedAssembly)
        {
            _totalCalls++;

            loadedAssembly = TryLoadAssembly(context, assemblyName);
            return loadedAssembly != null;
        }

        public abstract Assembly TryLoadAssembly(AssemblyLoadContext context, AssemblyName assemblyName);

        public void WriteReport()
        {
            Console.WriteLine();
            Console.WriteLine(GetType().Name);
            Console.WriteLine();
            Console.WriteLine($"Total Calls: {_totalCalls}");
        }
    }
}
#endif

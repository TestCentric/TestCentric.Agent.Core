// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using NUnitLite;

namespace TestCentric.agents
{
    class Program
    {
        static int Main(string[] args)
        {
            InternalTrace.Initialize(null, InternalTraceLevel.Off);
            return new AutoRun().Execute(args);
        }
    }
}

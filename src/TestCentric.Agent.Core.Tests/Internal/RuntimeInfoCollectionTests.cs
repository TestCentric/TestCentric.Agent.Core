// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if NETCOREAPP3_1_OR_GREATER
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestCentric.Engine.Internal
{
    public class RuntimeInfoCollectionTests
    {
        static RuntimeInfoCollection _runtimes = new RuntimeInfoCollection();

//        [Test]
        public static void CanGetRuntimesByName()
        {
            Assert.That(_runtimes.GetRuntimes("Microsoft.NETCore.App").Count, Is.GreaterThan(0));
        }
    }
}
#endif

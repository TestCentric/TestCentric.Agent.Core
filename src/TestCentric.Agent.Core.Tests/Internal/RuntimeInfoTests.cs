// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if NETCOREAPP3_1_OR_GREATER
using System;
using System.IO;
using NUnit.Framework;

namespace TestCentric.Engine.Internal
{
    public class RuntimeInfoTests
    {
        const string NAME = "RuntimeName";
        const string VERSION = "1.2.3";
        const string LOCATION = @"C:\Program Files\dotnet\RuntimeName";
        static readonly string FULL_PATH = Path.Combine(LOCATION, VERSION);
        const string INVALID_PATH = "Invalid%Path";

        [Test]
        public void Constructor()
        {
            var runtime = new RuntimeInfo(NAME, new Version(VERSION), FULL_PATH);

            Assert.Multiple(() =>
            {
                Assert.That(runtime.Name, Is.EqualTo(NAME));
                Assert.That(runtime.Version, Is.EqualTo(new Version(VERSION)));
                Assert.That(runtime.Location, Is.EqualTo(FULL_PATH));
            });
        }

        [TestCase(NAME + " " + VERSION + " [" + LOCATION + "]")]
        [TestCase(NAME + " " + VERSION + " " + LOCATION)]
        public void CreateFromTextSucceeds(string text)
        {
            var runtime = RuntimeInfo.FromText(text);

            Assert.Multiple(() =>
            {
                Assert.That(runtime.Name, Is.EqualTo(NAME));
                Assert.That(runtime.Version, Is.EqualTo(new Version(VERSION)));
                Assert.That(runtime.Location, Is.EqualTo(FULL_PATH));
            });
        }

        [TestCase(NAME + " [" + LOCATION + "]")]
        [TestCase(NAME + " " + LOCATION)]
        [TestCase(NAME + " " + VERSION)]
        [TestCase(NAME + " " + VERSION + INVALID_PATH, ExcludePlatform = "LINUX")]
        public void CreateFromText_Exceptions(string text)
        {
            Assert.That(() => RuntimeInfo.FromText(text),
                Throws.Exception);
        }
    }

}
#endif

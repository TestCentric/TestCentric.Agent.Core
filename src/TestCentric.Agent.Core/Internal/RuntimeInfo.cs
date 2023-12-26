// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if NETCOREAPP3_1_OR_GREATER
using System;
using System.IO;

namespace TestCentric.Engine.Internal
{
    public class RuntimeInfo
    {
        public string Name { get; }
        public Version Version { get; }
        public string Location { get; }

        public RuntimeInfo(string name, Version version, string location)
        {
            Name = name;
            Version = version;
            Location = location;
        }

        public static RuntimeInfo FromText(string text)
        {
            int index1 = text.IndexOf(' ');
            if (index1 < 0)
                throw new Exception("Invalid runtime info: " + text);

            int index2 = text.IndexOf(' ', index1 + 1);
            if (index2 < 0)
                throw new Exception("Invalid runtime info: " + text);

            var name = text.Substring(0, index1);
            var version = text.Substring(index1 + 1, index2 - index1 - 1);
            var basedir = text.Substring(index2 + 1).Trim(new[] { ' ', '[', ']' });
            var location = Path.Combine(basedir, version);
            //var location = basedir + Path.DirectorySeparatorChar + version;

            return new RuntimeInfo(name, new Version(version), location);
        }
    }
}
#endif

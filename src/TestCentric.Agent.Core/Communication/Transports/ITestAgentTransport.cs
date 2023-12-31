// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using TestCentric.Engine.Agents;

namespace TestCentric.Engine.Communication.Transports
{
    public interface ITestAgentTransport
    {
        TestAgent Agent { get; }
        ITestEngineRunner CreateRunner(TestPackage package);
        bool Start();
        void Stop();
    }
}

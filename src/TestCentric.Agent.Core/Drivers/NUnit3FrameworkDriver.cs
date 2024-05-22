// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TestCentric.Engine.Extensibility;
using TestCentric.Metadata;

namespace TestCentric.Engine.Drivers
{
    /// <summary>
    /// NUnitFrameworkDriver is used by the test-runner to load and run
    /// tests using the NUnit framework assembly.
    /// </summary>
    public class NUnit3FrameworkDriver : IFrameworkDriver
    {
        private const string LOAD_MESSAGE = "Method called without calling Load first";
        const string INVALID_FRAMEWORK_MESSAGE = "Running tests against this version of the framework using this driver is not supported. Please update NUnit.Framework to the latest version.";
        const string FAILED_TO_LOAD_TEST_ASSEMBLY = "Failed to load the test assembly {0}";
        const string FAILED_TO_LOAD_NUNIT = "Failed to load the NUnit Framework in the test assembly";

        private static readonly string CONTROLLER_TYPE = "NUnit.Framework.Api.FrameworkController";
        private static readonly string LOAD_ACTION = CONTROLLER_TYPE + "+LoadTestsAction";
        private static readonly string EXPLORE_ACTION = CONTROLLER_TYPE + "+ExploreTestsAction";
        private static readonly string COUNT_ACTION = CONTROLLER_TYPE + "+CountTestsAction";
        private static readonly string RUN_ACTION = CONTROLLER_TYPE + "+RunTestsAction";
        private static readonly string STOP_RUN_ACTION = CONTROLLER_TYPE + "+StopRunAction";

        static Logger log = InternalTrace.GetLogger("NUnitFrameworkDriver");

        string _testAssemblyPath;
        Assembly _testAssembly;
        Assembly _frameworkAssembly;
        object _frameworkController;

        public string ID { get; set; }

        /// <summary>
        /// Loads the tests in an assembly.
        /// </summary>
        /// <returns>An Xml string representing the loaded test</returns>
        public string Load(string testAssemblyPath, IDictionary<string, object> settings)
        {
            Guard.ArgumentValid(File.Exists(testAssemblyPath), "Framework driver constructor called with a file name that doesn't exist.", "testAssemblyPath");

            var idPrefix = string.IsNullOrEmpty(ID) ? "" : ID + "-";

            _testAssemblyPath = testAssemblyPath;
            var assemblyRef = AssemblyDefinition.ReadAssembly(testAssemblyPath);
            _testAssembly = Assembly.LoadFrom(testAssemblyPath);
            if (_testAssembly == null)
                throw new EngineException(string.Format(FAILED_TO_LOAD_TEST_ASSEMBLY, assemblyRef.FullName));

            // NOTE: We could use Linq here, but would still need the loop for NET20
            AssemblyNameReference nunitRef = null;
            foreach (var reference in assemblyRef.MainModule.AssemblyReferences)
                if (reference.Name.Equals("nunit.framework", StringComparison.OrdinalIgnoreCase))
                {
                    nunitRef = reference;
                    break;
                }

            if (nunitRef == null)
                throw new EngineException(FAILED_TO_LOAD_NUNIT);

            var nunit = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(testAssemblyPath), nunitRef.Name + ".dll"));
            if (nunit == null)
                throw new EngineException(FAILED_TO_LOAD_NUNIT);

            _frameworkAssembly = nunit;

            _frameworkController = CreateObject(CONTROLLER_TYPE, _testAssembly, idPrefix, settings);
            if (_frameworkController == null)
                throw new EngineException(INVALID_FRAMEWORK_MESSAGE);

            CallbackHandler handler = new CallbackHandler();

            var fileName = Path.GetFileName(_testAssemblyPath);
            log.Info("Loading {0} - see separate log file", fileName);
            CreateObject(LOAD_ACTION, _frameworkController, handler);
            log.Info("Loaded {0}", fileName);

            return handler.Result;
        }

        public int CountTestCases(string filter)
        {
            CheckLoadWasCalled();
            CallbackHandler handler = new CallbackHandler();
            CreateObject(COUNT_ACTION, _frameworkController, filter, handler);
            return int.Parse(handler.Result);
        }

        /// <summary>
        /// Executes the tests in an assembly.
        /// </summary>
        /// <param name="listener">An ITestEventHandler that receives progress notices</param>
        /// <param name="filter">A filter that controls which tests are executed</param>
        /// <returns>An Xml string representing the result</returns>
        public string Run(ITestEventListener listener, string filter)
        {
            CheckLoadWasCalled();

            var handler = new RunTestsCallbackHandler(listener);
            var filename = Path.GetFileName(_testAssemblyPath);
            log.Info("Running {0} - see separate log file", filename);
            CreateObject(RUN_ACTION, _frameworkController, filter, handler);
            return handler.Result;
        }

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        /// <remarks>
        /// The call with force:true is no longer supported. We throw rather than just ignoring it
        /// so that users will be aware of this important change and can modify their code accordingly.
        /// /// </remarks>
        public void StopRun(bool force)
        {
            if (force)
                throw new ArgumentException("StopRun with force:true is no longer supported");

            log.Info("Requesting stop");
            CreateObject(STOP_RUN_ACTION, _frameworkController, false, new CallbackHandler());
        }

        /// <summary>
        /// Returns information about the tests in an assembly.
        /// </summary>
        /// <param name="filter">A filter indicating which tests to include</param>
        /// <returns>An Xml string representing the tests</returns>
        public string Explore(string filter)
        {
            CheckLoadWasCalled();
            log.Info("Exploring {0} - see separate log file", Path.GetFileName(_testAssemblyPath));
            CallbackHandler handler = new CallbackHandler();
            CreateObject(EXPLORE_ACTION, _frameworkController, filter, handler);
            return handler.Result;
        }

        private void CheckLoadWasCalled()
        {
            if (_frameworkController == null)
                throw new InvalidOperationException(LOAD_MESSAGE);
        }

        private object CreateObject(string typeName, params object[] args)
        {
            var type = _frameworkAssembly.GetType(typeName);
            if (type == null)
                log.Error("Could not find type {typeName}");
            return Activator.CreateInstance(type, args);
        }
    }
}
#endif

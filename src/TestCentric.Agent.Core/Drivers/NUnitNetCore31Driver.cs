﻿// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if NETCOREAPP3_1_OR_GREATER
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TestCentric.Engine.Extensibility;
using TestCentric.Engine.Internal;

namespace TestCentric.Engine.Drivers
{
    /// <summary>
    /// NUnitNetCore31Driver is used by the test-runner to load and run
    /// tests using the NUnit framework assembly. It contains functionality to
    /// correctly load assemblies from other directories, using APIs first available in
    /// .NET Core 3.1.
    /// </summary>
    public class NUnitNetCore31Driver : IFrameworkDriver
    {
        const string LOAD_MESSAGE = "Method called without calling Load first";
        const string INVALID_FRAMEWORK_MESSAGE = "Running tests against this version of the framework using this driver is not supported. Please update NUnit.Framework to the latest version.";
        const string FAILED_TO_LOAD_TEST_ASSEMBLY = "Failed to load the test assembly {0}";
        const string FAILED_TO_LOAD_NUNIT = "Failed to load the NUnit Framework in the test assembly";

        static readonly string CONTROLLER_TYPE = "NUnit.Framework.Api.FrameworkController";
        static readonly string LOAD_METHOD = "LoadTests";
        static readonly string EXPLORE_METHOD = "ExploreTests";
        static readonly string COUNT_METHOD = "CountTests";
        static readonly string RUN_METHOD = "RunTests";
        static readonly string RUN_ASYNC_METHOD = "RunTests";
        static readonly string STOP_RUN_METHOD = "StopRun";

        static Logger log = InternalTrace.GetLogger(nameof(NUnitNetCore31Driver));

        Assembly _testAssembly;
        Assembly _frameworkAssembly;
        object _frameworkController;
        Type _frameworkControllerType;
        TestAssemblyLoadContext _assemblyLoadContext;

        private string _testAssemblyPath;
        private IDictionary<string, object> _testSettings;

        /// <summary>
        /// An id prefix that will be passed to the test framework and used as part of the
        /// test ids created.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Loads the tests in an assembly.
        /// </summary>
        /// <param name="assemblyPath">The path to the test assembly</param>
        /// <param name="settings">The test settings</param>
        /// <returns>An XML string representing the loaded test</returns>
        public string Load(string assemblyPath, IDictionary<string, object> settings)
        {
            _testAssemblyPath = Path.GetFullPath(assemblyPath);  //AssemblyLoadContext requires an absolute path
            _testSettings = settings;
            return CreateTestAssemblyContext();
        }

        private string CreateTestAssemblyContext()
        { 
            log.Debug($"Loading {_testAssemblyPath}");
            var idPrefix = string.IsNullOrEmpty(ID) ? "" : ID + "-";

            _assemblyLoadContext = new TestAssemblyLoadContext(_testAssemblyPath);

            try
            {
                _testAssembly = _assemblyLoadContext.LoadFromAssemblyPath(_testAssemblyPath);
            }
            catch (Exception e)
            {
                var msg = string.Format(FAILED_TO_LOAD_TEST_ASSEMBLY, _testAssemblyPath);
                log.Error(msg);
                throw new EngineException(msg, e);
            }
            log.Debug($"Loaded {_testAssemblyPath}");

            var nunitRef = _testAssembly.GetReferencedAssemblies().FirstOrDefault(reference => reference.Name.Equals("nunit.framework", StringComparison.OrdinalIgnoreCase));
            if (nunitRef == null)
            {
                log.Error(FAILED_TO_LOAD_NUNIT);
                throw new EngineException(FAILED_TO_LOAD_NUNIT);
            }

            try
            {
                _frameworkAssembly = _assemblyLoadContext.LoadFromAssemblyName(nunitRef);
            }
            catch (Exception e)
            {
                log.Error($"{FAILED_TO_LOAD_NUNIT}\r\n{e}");
                throw new EngineException(FAILED_TO_LOAD_NUNIT, e);
            }
            log.Debug("Loaded nunit.framework");

            _frameworkController = CreateObject(CONTROLLER_TYPE, _testAssembly, idPrefix, _testSettings);
            if (_frameworkController == null)
            {
                log.Error(INVALID_FRAMEWORK_MESSAGE);
                throw new EngineException(INVALID_FRAMEWORK_MESSAGE);
            }

            _frameworkControllerType = _frameworkController.GetType();
            log.Debug($"Created FrameworkControler {_frameworkControllerType.Name}");

            log.Info("Loading {0} - see separate log file", _testAssembly.FullName);
            return ExecuteMethod(LOAD_METHOD) as string;
        }

        /// <summary>
        /// Counts the number of test cases for the loaded test assembly
        /// </summary>
        /// <param name="filter">The XML test filter</param>
        /// <returns>The number of test cases</returns>
        public int CountTestCases(string filter)
        {
            CreateTestAssemblyContext();
            object count = ExecuteMethod(COUNT_METHOD, filter);
            UnloadAssemblyContext();
            return count != null ? (int)count : 0;
        }

        /// <summary>
        /// Executes the tests in an assembly.
        /// </summary>
        /// <param name="listener">An ITestEventHandler that receives progress notices</param>
        /// <param name="filter">A filter that controls which tests are executed</param>
        /// <returns>An Xml string representing the result</returns>
        public string Run(ITestEventListener listener, string filter)
        {
            CreateTestAssemblyContext();

            log.Info("Running {0} - see separate log file", _testAssembly.FullName);
            Action<string> callback = listener != null ? listener.OnTestEvent : (Action<string>)null;
            var result = ExecuteMethod(RUN_METHOD, new[] { typeof(Action<string>), typeof(string) }, callback, filter) as string;

            UnloadAssemblyContext();
            return result;
        }

        /// <summary>
        /// Executes the tests in an assembly asynchronously.
        /// </summary>
        /// <param name="callback">A callback that receives XML progress notices</param>
        /// <param name="filter">A filter that controls which tests are executed</param>
        public void RunAsync(Action<string> callback, string filter)
        {
            CreateTestAssemblyContext();

            log.Info("Running {0} - see separate log file", _testAssembly.FullName);
            ExecuteMethod(RUN_ASYNC_METHOD, new[] { typeof(Action<string>), typeof(string) }, callback, filter);
        }

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public void StopRun(bool force)
        {
            ExecuteMethod(STOP_RUN_METHOD, force);
        }

        /// <summary>
        /// Returns information about the tests in an assembly.
        /// </summary>
        /// <param name="filter">A filter indicating which tests to include</param>
        /// <returns>An Xml string representing the tests</returns>
        public string Explore(string filter)
        {
            CheckLoadWasCalled();

            log.Info("Exploring {0} - see separate log file", _testAssembly?.FullName);
            var result = ExecuteMethod(EXPLORE_METHOD, filter) as string;
            UnloadAssemblyContext();

            return result;
        }

        void CheckLoadWasCalled()
        {
            if (_frameworkController == null)
                throw new InvalidOperationException(LOAD_MESSAGE);
        }

        private void UnloadAssemblyContext()
        {
            _frameworkController = null;
            _frameworkAssembly = null;
            _frameworkControllerType = null;
            _testAssembly = null;
            _assemblyLoadContext?.Unload();
            _assemblyLoadContext = null;
        }

        object CreateObject(string typeName, params object[] args)
        {
            var typeinfo = _frameworkAssembly.DefinedTypes.FirstOrDefault(t => t.FullName == typeName);
            if (typeinfo == null)
            {
                log.Error("Could not find type {0}", typeName);
            }
            return Activator.CreateInstance(typeinfo.AsType(), args);
        }

        object ExecuteMethod(string methodName, params object[] args)
        {
            var method = _frameworkControllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
                log.Error($"Method {methodName} was not found in {_frameworkControllerType.Name}");
            log.Debug($"Executing {method.DeclaringType}.{method.Name}");
            return ExecuteMethod(method, args);
        }

        object ExecuteMethod(string methodName, Type[] ptypes, params object[] args)
        {
            var method = _frameworkControllerType.GetMethod(methodName, ptypes);
            return ExecuteMethod(method, args);
        }

        object ExecuteMethod(MethodInfo method, params object[] args)
        {
            if (method == null)
            {
                throw new EngineException(INVALID_FRAMEWORK_MESSAGE);
            }

            using (_assemblyLoadContext.EnterContextualReflection())
            {
                return method.Invoke(_frameworkController, args);
            }
        }
    }
}
#endif
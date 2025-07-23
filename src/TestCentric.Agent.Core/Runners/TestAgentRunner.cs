// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using TestCentric.Engine.Drivers;
using TestCentric.Engine.Internal;
using TestCentric.Engine.Extensibility;
using System.ComponentModel;
using System.Collections.Generic;

namespace TestCentric.Engine.Runners
{
    /// <summary>
    /// TestAgentRunner is the abstract base for runners used by agents, which
    /// deal directly with a framework driver. It loads and runs tests in a single
    /// assembly, creating an <see cref="IFrameworkDriver"/> to do so.
    /// </summary>
    public abstract class TestAgentRunner : ITestEngineRunner
    {
        // TestAgentRunner loads and runs tests in a particular AppDomain using
        // one driver per assembly. All test assemblies are ultimately executed by
        // an agent using one of its derived classes, either LocalTestRunner
        // or TestDomainRunner.
        //
        // TestAgentRunner creates an appropriate framework driver for the assembly
        // specified in the TestPackage.

        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAgentRunner));

        private IFrameworkDriver _driver;

        private ProvidedPathsAssemblyResolver _assemblyResolver;

        protected AppDomain TestDomain { get; set; }

        // Used to inject DriverService for testing
        internal IDriverService DriverService { get; set; }

        public TestAgentRunner(TestPackage package)
        {
            Guard.ArgumentNotNull(package, nameof(package));
            Guard.ArgumentValid(package.SubPackages.Count == 0, "Only one assembly may be loaded by an agent", nameof(package));
            Guard.ArgumentValid(package.FullName != null, "Package may not be anonymous", nameof(package));
            Guard.ArgumentValid(package.IsAssemblyPackage, "Must be an assembly package", nameof(package));

            TestPackage = package;

            // Bypass the resolver if not in the default AppDomain. This prevents trying to use the resolver within
            // NUnit's own automated tests (in a test AppDomain) which does not make sense anyway.
            if (AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                _assemblyResolver = new ProvidedPathsAssemblyResolver();
                _assemblyResolver.Install();
            }
        }

        /// <summary>
        /// The TestPackage, which this runner is handling
        /// </summary>
        protected TestPackage TestPackage { get; set; }

        /// <summary>
        /// The result of the last call to LoadPackage
        /// for this runner's TestPackage.
        /// </summary>
        protected TestEngineResult LoadResult { get; set; }

        /// <summary>
        /// Indicates whether the runner's TestPackage is loaded.
        /// </summary>
        public bool IsPackageLoaded => LoadResult != null;

        #region ITestEngineRunner Implementation

        /// <summary>
        /// Loads the TestPackage for exploration or execution,
        /// saving the result. Overridden in derived classes to 
        /// take any necessary action before or after loading.
        /// </summary>
        /// <returns>A TestEngineResult.</returns>
        public virtual TestEngineResult Load()
        {
            var testFile = TestPackage.FullName;
            log.Info($"Loading package {testFile}");

            string targetFramework = TestPackage.Settings.GetValueOrDefault(SettingDefinitions.ImageTargetFrameworkName);
            string frameworkReference = TestPackage.Settings.GetValueOrDefault(SettingDefinitions.ImageTestFrameworkReference);
            bool skipNonTestAssemblies = TestPackage.Settings.GetValueOrDefault(SettingDefinitions.SkipNonTestAssemblies);

            if (DriverService == null)
                DriverService = new DriverService();

            if (_assemblyResolver != null && !TestDomain.IsDefaultAppDomain()
                && TestPackage.Settings.GetValueOrDefault(SettingDefinitions.ImageRequiresDefaultAppDomainAssemblyResolver))
            {
                // It's OK to do this in the loop because the Add method
                // checks to see if the path is already present.
                _assemblyResolver.AddPathFromFile(testFile);
            }

            log.Debug("Getting agent from DriverService");
            _driver = DriverService.GetDriver(TestDomain, testFile, targetFramework, skipNonTestAssemblies);
            _driver.ID = TestPackage.ID;
            log.Debug($"Using driver {_driver.GetType().Name}");

            var frameworkSettings = new Dictionary<string, object>();
            foreach (var setting in TestPackage.Settings)
                frameworkSettings.Add(setting.Name, setting.Value);

            try
            {
                return LoadResult = new TestEngineResult(_driver.Load(testFile, frameworkSettings));
            }
            catch (Exception ex) when (!(ex is EngineException))
            {
                throw new EngineException("An exception occurred in the driver while loading tests.", ex);
            }
        }

        /// <summary>
        /// Reload the currently loaded test package, saving the result.
        /// </summary>
        /// <returns>A TestEngineResult.</returns>
        /// <exception cref="InvalidOperationException">If no package has been loaded</exception>
        public TestEngineResult Reload()
        {
            if (this.TestPackage == null)
                throw new InvalidOperationException("MasterTestRunner: Reload called before Load");

            // Currently, we just do a load
            return Load();
        }

        /// <summary>
        /// Unload any loaded TestPackage. Overridden in
        /// derived classes to take any necessary action.
        /// </summary>
        public virtual void Unload()
        {
            LoadResult = null;
        }

        /// <summary>
        /// Explores a previously loaded TestPackage and returns information
        /// about the tests found.
        /// </summary>
        /// <param name="filter">The TestFilter to be used to select tests</param>
        /// <returns>
        /// A TestEngineResult.
        /// </returns>
        public TestEngineResult Explore(TestFilter filter)
        {
            EnsurePackageIsLoaded();

            try
            {
                return new TestEngineResult(_driver.Explore(filter.Text));
            }
            catch (Exception ex) when (!(ex is EngineException))
            {
                throw new EngineException("An exception occurred in the driver while exploring tests.", ex);
            }
            finally
            {
                RunGarbageCollector();
            }

        }

        /// <summary>
        /// Count the test cases that would be run under
        /// the specified filter.
        /// </summary>
        /// <param name="filter">A TestFilter</param>
        /// <returns>The count of test cases</returns>
        public int CountTestCases(TestFilter filter)
        {
            EnsurePackageIsLoaded();

            try
            {
                return _driver.CountTestCases(filter.Text);
            }
            catch (Exception ex) when (!(ex is EngineException))
            {
                throw new EngineException("An exception occurred in the driver while counting test cases.", ex);
            }
            finally
            {
                RunGarbageCollector();
            }
        }

        /// <summary>
        /// Run the tests in the TestPackage, loading the package
        /// if this has not already been done.
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>A TestEngineResult giving the result of the test execution</returns>
        public TestEngineResult Run(ITestEventListener listener, TestFilter filter)
        {
            return RunTests(listener, filter);
        }

        /// <summary>
        /// Start a run of the tests in the loaded TestPackage, returning immediately.
        /// The tests are run asynchronously and the listener interface is notified
        /// as it progresses.
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>An <see cref="AsyncTestEngineResult"/> that will provide the result of the test execution</returns>
        public AsyncTestEngineResult RunAsync(ITestEventListener listener, TestFilter filter)
        {
            var testRun = new AsyncTestEngineResult();

            using (var worker = new BackgroundWorker())
            {
                worker.DoWork += (s, ea) =>
                {
                    var result = RunTests(listener, filter);
                    testRun.SetResult(result);
                };
                worker.RunWorkerAsync();
            }

            return testRun;
        }

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public void RequestStop()
        {
            log.Info("Requesting stop");

            EnsurePackageIsLoaded();

            try
            {
                _driver.StopRun(false);
            }
            catch (Exception ex) when (!(ex is EngineException))
            {
                throw new EngineException("An exception occurred in the driver while stopping the run.", ex);
            }
        }

        public void ForcedStop()
        {
            log.Info("Cancelling test run");
            Environment.Exit(0);
        }

        #endregion

        /// <summary>
        /// Run the tests in the loaded TestPackage.
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>
        /// A TestEngineResult giving the result of the test execution
        /// </returns>
        private TestEngineResult RunTests(ITestEventListener listener, TestFilter filter)
        {
            log.Info($"Running tests for {TestPackage.FullName}");

            EnsurePackageIsLoaded();

            string driverResult;

            if (filter.Excludes(TestPackage))
                driverResult = $"<test-suite type='Assembly' name='{TestPackage.Name}' fullname='{TestPackage.FullName}' result='Skipped'><reason><message>Filter excludes this assembly</message></reason></test-suite>";
            else
                try
                {
                    driverResult = _driver.Run(listener, filter.Text);
                    log.Debug("Got driver Result");
                }
                catch (Exception ex) when (!(ex is EngineException))
                {
                    log.Debug("An exception occurred in the driver while running tests.", ex);
                    throw new EngineException("An exception occurred in the driver while running tests.", ex);
                }
                finally
                {
                    RunGarbageCollector();
                }

            if (_assemblyResolver != null)
                _assemblyResolver.RemovePathFromFile(TestPackage.FullName);

            return new TestEngineResult(driverResult);
        }

        private void EnsurePackageIsLoaded()
        {
            if (!IsPackageLoaded)
                Load();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    Unload();

                _disposed = true;
            }
        }

        /// <summary>
        /// This garbage collector call is required for the unloading of a AssemblyLoadContext
        /// </summary>
        private static void RunGarbageCollector()
        {
            for (int i = 0; i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}

// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if NETFRAMEWORK
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Security.Principal;

namespace TestCentric.Engine.Internal
{
    /// <summary>
    /// The DomainManager class handles the creation and unloading
    /// of domains as needed and keeps track of all existing domains.
    /// </summary>
    public class DomainManager
    {
        static Logger log = InternalTrace.GetLogger(typeof(DomainManager));

        private static readonly PropertyInfo TargetFrameworkNameProperty =
            typeof(AppDomainSetup).GetProperty("TargetFrameworkName", BindingFlags.Public | BindingFlags.Instance);

        private TestPackage _package;

        /// <summary>
        /// Construct an application domain for running a test package
        /// </summary>
        /// <param name="package">The TestPackage to be run</param>
        public AppDomain CreateDomain( TestPackage package )
        {
            // Save package for use when we unload
            _package = package;

            AppDomainSetup setup = CreateAppDomainSetup(package);

            string hashCode = string.Empty;
            if (package.Name != null)
            {
                hashCode = package.Name.GetHashCode().ToString("x") + "-";
            }

            string domainName = "domain-" + hashCode + package.Name;

            log.Info("Creating application domain " + domainName);

            AppDomain runnerDomain = AppDomain.CreateDomain(domainName, /*evidence*/null, setup);

            // Set PrincipalPolicy for the domain if called for in the package settings
            if (package.Settings.HasSetting(SettingDefinitions.PrincipalPolicy))
            {
                PrincipalPolicy policy = (PrincipalPolicy)Enum.Parse(typeof(PrincipalPolicy),
                    package.Settings.GetValueOrDefault(SettingDefinitions.PrincipalPolicy));

                runnerDomain.SetPrincipalPolicy(policy);
            }

            return runnerDomain;
        }

        // Made separate and internal for testing
        AppDomainSetup CreateAppDomainSetup(TestPackage package)
        {
            AppDomainSetup setup = new AppDomainSetup();

            //For parallel tests, we need to use distinct application name
            setup.ApplicationName = "Tests" + "_" + Environment.TickCount;

            string appBase = GetApplicationBase(package);
            setup.ApplicationBase = appBase;
            setup.ConfigurationFile = GetConfigFile(appBase, package);
            setup.PrivateBinPath = GetPrivateBinPath(appBase, package);

            if (!string.IsNullOrEmpty(package.FullName))
            {
                // Setting the target framework is only supported when running with
                // multiple AppDomains, one per assembly.
                // TODO: Remove this limitation

                // .NET versions greater than v4.0 report as v4.0, so look at
                // the TargetFrameworkAttribute on the assembly if it exists
                // If property is null, .NET 4.5+ is not installed, so there is no need
                if (TargetFrameworkNameProperty != null)
                {
                    var frameworkName = package.Settings.GetValueOrDefault(SettingDefinitions.ImageTargetFrameworkName);
                    if (frameworkName != "")
                        TargetFrameworkNameProperty.SetValue(setup, frameworkName, null);
                }
            }

            if (package.Settings.GetValueOrDefault(SettingDefinitions.ShadowCopyFiles))
            {
                setup.ShadowCopyFiles = "true";
                setup.ShadowCopyDirectories = setup.ApplicationBase;
            }
            else
                setup.ShadowCopyFiles = "false";

            return setup;
        }

        public void Unload(AppDomain domain)
        {
            new DomainUnloader(domain).Unload(_package);
        }

        class DomainUnloader
        {
            private readonly AppDomain _domain;
            private Thread _unloadThread;
            private EngineException _unloadException;

            public DomainUnloader(AppDomain domain)
            {
                _domain = domain;
            }

            private bool _simulateUnloadError;
            private bool _simulateUnloadTimeout;

            public void Unload(TestPackage package)
            {
                _simulateUnloadError = package.Settings.GetValueOrDefault(SettingDefinitions.SimulateUnloadError);
                _simulateUnloadTimeout = package.Settings.GetValueOrDefault(SettingDefinitions.SimulateUnloadTimeout);

                _unloadThread = new Thread(new ThreadStart(UnloadOnThread));
                _unloadThread.Start();

                var timeout = TimeSpan.FromSeconds(30);

                if (!_unloadThread.Join((int)timeout.TotalMilliseconds))
                {
                    var msg = DomainDetailsBuilder.DetailsFor(_domain,
                        $"Unable to unload application domain: unload thread timed out after {timeout.TotalSeconds} seconds.");

                    log.Error(msg);
                    Kill(_unloadThread);

                    throw new EngineUnloadException(msg);
                }

                if (_unloadException != null)
                    throw new EngineUnloadException("Exception encountered unloading application domain", _unloadException);
            }

            private void UnloadOnThread()
            {
                try
                {
                    if (_simulateUnloadError)
                        throw new CannotUnloadAppDomainException("Testing: simulated unload error");

                    if (_simulateUnloadTimeout)
                        while (true) ;

                    AppDomain.Unload(_domain);
                }
                catch (Exception ex)
                {
                    // We assume that the tests did something bad and just leave
                    // the orphaned AppDomain "out there".
                    var msg = DomainDetailsBuilder.DetailsFor(_domain,
                        $"Exception encountered unloading application domain: {ex.Message}");

                    _unloadException = new EngineException(msg);
                    log.Error(msg);
                }
            }
        }

        /// <summary>
        /// Figure out the ApplicationBase for a package
        /// </summary>
        /// <param name="package">The package</param>
        /// <returns>The ApplicationBase</returns>
        public static string GetApplicationBase(TestPackage package)
        {
            Guard.ArgumentNotNull(package, "package");

            var appBase = package.Settings.GetValueOrDefault(SettingDefinitions.BasePath);

            if (string.IsNullOrEmpty(appBase))
                appBase = string.IsNullOrEmpty(package.FullName)
                    ? GetCommonAppBase(package.SubPackages)
                    : Path.GetDirectoryName(package.FullName);

            if (!string.IsNullOrEmpty(appBase))
            {
                char lastChar = appBase[appBase.Length - 1];
                if (lastChar != Path.DirectorySeparatorChar && lastChar != Path.AltDirectorySeparatorChar)
                    appBase += Path.DirectorySeparatorChar;
            }

            return appBase;
        }

        public static string GetConfigFile(string appBase, TestPackage package)
        {
            Guard.ArgumentNotNullOrEmpty(appBase, "appBase");
            Guard.ArgumentNotNull(package, "package");

            // Use provided setting if available
            string configFile = package.Settings.GetValueOrDefault(SettingDefinitions.ConfigurationFile);
            if (configFile != string.Empty)
                return Path.Combine(appBase, configFile);

            // The ProjectService adds any project config to the settings.
            // So, at this point, we only want to handle assemblies or an
            // anonymous package created from the command-line.
            string fullName = package.FullName;
            if (IsExecutable(fullName))
                return fullName + ".config";

            // Command-line package gets no config unless it's a single assembly
            if (string.IsNullOrEmpty(fullName) && package.SubPackages.Count == 1)
            {
                fullName = package.SubPackages[0].FullName;
                if (IsExecutable(fullName))
                    return fullName + ".config";
            }

            // No config file will be specified
            return null;
        }

        private static bool IsExecutable(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            string ext = Path.GetExtension(fileName).ToLower();
            return ext == ".dll" || ext == ".exe";
        }

        public static string GetCommonAppBase(IList<TestPackage> packages)
        {
            var assemblies = new List<string>();
            foreach (var package in packages)
                assemblies.Add(package.FullName);

            return GetCommonAppBase(assemblies);
        }

        public static string GetCommonAppBase(IList<string> assemblies)
        {
            string commonBase = null;

            foreach (string assembly in assemblies)
            {
                string dir = Path.GetDirectoryName(Path.GetFullPath(assembly));
                if (commonBase == null)
                    commonBase = dir;
                else while (!PathUtils.SamePathOrUnder(commonBase, dir) && commonBase != null)
                        commonBase = Path.GetDirectoryName(commonBase);
            }

            return commonBase;
        }

        public static string GetPrivateBinPath(string basePath, string fileName)
        {
            return GetPrivateBinPath(basePath, new string[] { fileName });
        }

        public static string GetPrivateBinPath(string appBase, TestPackage package)
        {
            var binPath = package.Settings.GetValueOrDefault(SettingDefinitions.PrivateBinPath);

            if (string.IsNullOrEmpty(binPath))
                binPath = package.SubPackages.Count > 0
                    ? GetPrivateBinPath(appBase, package.SubPackages)
                    : package.FullName != null
                        ? GetPrivateBinPath(appBase, package.FullName)
                        : null;

            return binPath;
        }

        public static string GetPrivateBinPath(string basePath, IList<TestPackage> packages)
        {
            var assemblies = new List<string>();
            foreach (var package in packages)
                assemblies.Add(package.FullName);

            return GetPrivateBinPath(basePath, assemblies);
        }

        public static string GetPrivateBinPath(string basePath, IList<string> assemblies)
        {
            List<string> dirList = new List<string>();
            StringBuilder sb = new StringBuilder(200);

            foreach( string assembly in assemblies )
            {
                string dir = PathUtils.RelativePath(
                    Path.GetFullPath(basePath),
                    Path.GetDirectoryName( Path.GetFullPath(assembly) ) );
                if ( dir != null && dir != string.Empty && dir != "." && !dirList.Contains( dir ) )
                {
                    dirList.Add( dir );
                    if ( sb.Length > 0 )
                        sb.Append( Path.PathSeparator );
                    sb.Append( dir );
                }
            }

            return sb.Length == 0 ? null : sb.ToString();
        }

        /// <summary>
        /// Do our best to kill a thread, passing state info
        /// </summary>
        /// <param name="thread">The thread to kill</param>
        private static void Kill(Thread thread)
        {
            try
            {
                thread.Abort();
            }
            catch (ThreadStateException)
            {
                // Although obsolete, this use of Resume() takes care of
                // the odd case where a ThreadStateException is received.
#pragma warning disable 0618, 0612    // Thread.Resume has been deprecated
                thread.Resume();
#pragma warning restore 0618, 0612   // Thread.Resume has been deprecated
            }

            if ((thread.ThreadState & System.Threading.ThreadState.WaitSleepJoin) != 0)
                thread.Interrupt();
        }
    }
}
#endif

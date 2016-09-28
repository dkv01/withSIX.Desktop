// <copyright company="SIX Networks GmbH" file="Common.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

#if MAIN_RELEASE
#elif BETA_RELEASE
#elif NIGHTLY_RELEASE
#define STAGING
#elif DEBUG
#define STAGING
#define DEV_BUILD
#else
#define DEV_BUILD
#endif

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using MediatR;
using NDepend.Path;
using SN.withSIX.Core.Logging;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Core
{
    public enum ReleaseType
    {
        Dev,
        Alpha,
        Beta,
        Stable
    }

    public static class Common
    {
        public const string DefaultCategory = "Unknown";
        public const long MagicPingValue = 9999;
        public const string ClientHeader = "X-Six-Client";
        public const string ClientHeaderV = ClientHeader + "-V";
        public static AppCommon App;
        public static StartupFlags Flags { get; set; }
        public static DateTime StartTime { get; } = Process.GetCurrentProcess().StartTime.ToUniversalTime();
        public static readonly string[] DefaultHosts = {
            "c1-de.sixmirror.com",
            "c1-us.sixmirror.com"
        };
        // TODO: Premium host authentication needs to be addressed more dynamically
        public static readonly string[] PremiumHosts = {
            "c1-de-p.sixmirror.com",
            "c1-us-p.sixmirror.com",
            "c1-sg-p.sixmirror.com" // backwards compat, to make auth work
        };
        public static readonly string[] PremiumMirrors = PremiumHosts.Select(x => "zsync://" + x).ToArray();
        public static readonly string[] DefaultMirrors = DefaultHosts.Select(x => "zsync://" + x).ToArray();

        static Common() {
            App = new AppCommon();
        }

        public static PathConfiguration Paths { get; set; }
        public static Action OnExit { get; set; }
        public static bool IsMini { get; set; }
        public static string ReleaseTitle { get; set; } = "DEV";

        public static bool IsDebugEnabled =>
#if DEBUG
            true;
#else
        false;
#endif

        public class AppCommon : IEnableLogging
        {
            public const string ApplicationState = ""; //"Preview";
            public const string Classes = "Software\\Classes";
            public const int InitializationLvl = 1;
            public const string DefaultModResourceFile = "ModsPlaceholder-small40x40.png";
            public const string DefaultModResourceFileLarge = "ModsPlaceholder-full232x112.png";
            public const string DefaultModResourceFileHuge = "ModsPlaceholder-huge300x144.png";
            const string OnlineResourcePath = "https://d2l9k1uqfxdpqe.cloudfront.net/assets/mods/";
            static readonly bool debug;
            static readonly bool trace;
            public static readonly TimeSpan DefaultFilterDelay = TimeSpan.FromMilliseconds(250);
            public static bool IsBusy;
            public static readonly ReleaseType Type =
#if BETA_RELEASE
                ReleaseType.Beta;
#else
#if MAIN_RELEASE
                ReleaseType.Stable;
#else
#if NIGHTLY_RELEASE
                ReleaseType.Alpha;
#else
                ReleaseType.Dev;
#endif
#endif
#endif
            public static readonly string TitleType = Type == ReleaseType.Stable ? "" : " " + Type.ToString().ToUpper();

            static AppCommon() {
#if DEBUG
                debug = true;
#endif
#if TRACE
                trace = true;
#endif
            }

            public static string ApplicationName { get; private set; } = "Play withSIX";
            public static string ApplicationRegKey { get; private set; } = "Software\\SIX Networks\\" + ApplicationName;

            public Version ApplicationVersion { get; private set; }
            public string ProductVersion { get; private set; }
            public string AppTitle { get; private set; }

            public static void SetAppName(string appName) {
                ApplicationName = appName;
                ApplicationRegKey = "Software\\SIX Networks\\" + ApplicationName;
            }

            public void Init(string appName) {
                if (Flags.Staging || Flags.Portable)
                    InitLocal(appName);
                else
                    InitInternal(appName);
            }

            void InitLocal(string appName) {
                var tup = GetLocalPathData(CommonBase.AssemblyLoader.GetEntryPath());
                InitInternal(appName, tup.Item1, tup.Item2, tup.Item3, tup.Item4, null, null, tup.Item5);
            }

            public void InitLocalWithCleanup(string appName) {
                var tup = GetLocalPathData(CommonBase.AssemblyLoader.GetEntryPath());
                CleanPaths(tup.Item2, tup.Item3, tup.Item4, tup.Item5);
                InitInternal(appName, tup.Item1, tup.Item2, tup.Item3, tup.Item4, null, null, tup.Item5);
            }

            static
                Tuple
                <IAbsoluteDirectoryPath, IAbsoluteDirectoryPath, IAbsoluteDirectoryPath, IAbsoluteDirectoryPath,
                    IAbsoluteDirectoryPath> GetLocalPathData(IAbsoluteDirectoryPath localBasePath) {
                var dataDir = localBasePath.GetChildDirectoryWithName("Data");
                return Tuple.Create(localBasePath, dataDir.GetChildDirectoryWithName("RoamingData"),
                    dataDir.GetChildDirectoryWithName("LocalData"),
                    dataDir.GetChildDirectoryWithName("Temp"),
                    dataDir.GetChildDirectoryWithName("LocalData SIX Networks").GetChildDirectoryWithName("Shared"));
            }

            static void CleanPaths(params IAbsoluteDirectoryPath[] paths) {
                foreach (var path in paths) {
                    if (path.Exists)
                        Tools.FileUtil.Ops.DeleteWithRetry(path.ToString());
                    path.MakeSurePathExists();
                }
            }

            public static void ClearAwesomiumCache() {
                var cachePath = Paths.AwesomiumPath.GetChildDirectoryWithName("Cache");
                if (cachePath.Exists)
                    Tools.FileUtil.Ops.DeleteDirectory(cachePath);
                Paths.AwesomiumPath.MakeSurePathExists();
            }

            public static void ClearAwesomium() {
                var path = Paths.AwesomiumPath;
                if (path.Exists)
                    Tools.FileUtil.Ops.DeleteDirectory(path);
                Paths.AwesomiumPath.MakeSurePathExists();
            }

            void InitInternal(string appName = null,
                IAbsoluteDirectoryPath appPath = null, IAbsoluteDirectoryPath dataPath = null,
                IAbsoluteDirectoryPath localDataPath = null, IAbsoluteDirectoryPath tempPath = null,
                IAbsoluteDirectoryPath configPath = null,
                IAbsoluteDirectoryPath toolPath = null, IAbsoluteDirectoryPath sharedDataPath = null) {
                HandleAppInfo(appName);
                HandlePaths(appPath, dataPath, localDataPath, tempPath, configPath, toolPath, sharedDataPath);
                ShowInfo();
            }

            void HandleAppInfo(string appName) {
                if (appName == null)
                    appName = ApplicationName;
                AppTitle = appName;
                ApplicationVersion = CommonBase.AssemblyLoader.GetEntryVersion();
                ProductVersion = CommonBase.AssemblyLoader.GetProductVersion();
            }

            static void HandlePaths(IAbsoluteDirectoryPath appPath, IAbsoluteDirectoryPath dataPath,
                IAbsoluteDirectoryPath localDataPath, IAbsoluteDirectoryPath tempPath,
                IAbsoluteDirectoryPath configPath,
                IAbsoluteDirectoryPath toolPath,
                IAbsoluteDirectoryPath sharedDataPath) {
                if (!Paths.PathsSet)
                    Paths.SetPaths(appPath, dataPath, localDataPath, tempPath, configPath, toolPath, sharedDataPath);

                if (!Paths.PathsSet)
                    throw new Exception("Paths not set yet!");

                Paths.DataPath.MakeSurePathExists();
                Paths.LocalDataPath.MakeSurePathExists();
                Paths.LogPath.MakeSurePathExists();
                Paths.TempPath.MakeSurePathExists();
            }

            [Obsolete]
            void ShowInfo() {
                /*
                var elevated = TryGetElevatedStatus();

                this.Logger().Info("{0}, {1}, DEBUG: {2}, TRACE: {3}\n"
                                   + "OS: {4} 64-bit: {5}, .NET: {6}, CULT: {7}, Elevated: {8}, DefaultEncoding: {9}\n"
                                   + "Current dir: {10}\n"
                                   + "App: {11}\n"
                                   + "Data: {12}, LocalData: {13}\n"
                                   + "Arguments: {14}",
                    CommonBase.AssemblyLoader.GetEntryVersion(), TitleType,
                    debug, trace, Environment.OSVersion.Version, Environment.Is64BitOperatingSystem,
                    Environment.Version, CultureInfo.CurrentCulture, elevated, Encoding.Default,
                    Tools.FileUtil.FilterPath(Directory.GetCurrentDirectory()),
                    Tools.FileUtil.FilterPath(Paths.AppPath), Tools.FileUtil.FilterPath(Paths.DataPath),
                    Tools.FileUtil.FilterPath(Paths.LocalDataPath),
                    Tools.FileUtil.FilterPath(Flags.FullStartupParameters.CombineParameters()));
                    */
            }

            bool TryGetElevatedStatus() {
                var elevated = false;
                try {
                    elevated = Tools.UacHelper.IsProcessElevated();
                } catch (Exception e) {
                    this.Logger().FormattedDebugException(e);
                }
                return elevated;
            }

            public string GetResourcePath(string resource) {
                Contract.Requires<ArgumentNullException>(resource != null);

                if (resource.StartsWith("//"))
                    resource = "http:" + resource;

                if (Uri.IsWellFormedUriString(resource, UriKind.Absolute))
                    return resource;
                return OnlineResourcePath + resource;
            }
        }

        public class StartupFlags
        {
            public StartupFlags(string[] args, bool is64BitOperatingSystem) {
                var staging = false;
                var verbose = true;
                var selfUpdateSupported = true;
                var autoUpdateEnabled = true;
                var lockDown = false;
                FullStartupParameters = args;
                Is64BitOperatingSystem = is64BitOperatingSystem;
                if (args.Contains("--staging")) {
                    staging = true;
                    args = args.Where(x => !x.Equals("--staging")).ToArray();
                }

                if (args.Contains("--portable")) {
                    Portable = true;
                    args = args.Where(x => !x.Equals("--portable")).ToArray();
                }

                if (args.Contains("--verbose")) {
                    verbose = true;
                    args = args.Where(x => !x.Equals("--verbose")).ToArray();
                }

                if (args.Contains("--public"))
                    Public = true;

                if (args.Contains("--production"))
                    UseProduction = true;

                if (args.Contains("--skip-autoupdate")) {
                    autoUpdateEnabled = false;
                    args = args.Where(x => !x.Equals("--skip-autoupdate")).ToArray();
                }

                if (args.Contains("--ignore-error-dialogs")) {
                    IgnoreErrorDialogs = true;
                    args = args.Where(x => !x.Equals("--ignore-error-dialogs")).ToArray();
                }

                if (args.Contains("--skip-execution-confirmation")) {
                    SkipExecutionConfirmation = true;
                    args = args.Where(x => !x.Equals("--skip-execution-confirmation")).ToArray();
                }

                string lockDownModSet = null;
                var ld = args.FirstOrDefault(x => x.Contains("lockdown=true"));
                if (ld != null) {
                    args = args.Where(x => !x.Equals(ld)).ToArray();
                    var dic = Tools.Transfer.GetDictionaryFromQueryString(ld);
                    lockDown = true;
                    lockDownModSet = dic["mod_set"];
                }

                StartupParameters = args;

                /*
#if DEBUG
                                UseLocalServer = true;
#endif
*/

#if DEV_BUILD
                autoUpdateEnabled = false;
                selfUpdateSupported = false;
#endif

#if MAIN_RELEASE
                IsStable = true;
#endif

#if DEBUG || (!MAIN_RELEASE && !BETA_RELEASE)
                // This is used also to determine if the local withSIX-Updater/SelfUpdater should be used...
                IsInternal = true;
#endif

#if STAGING
                staging = true;
                verbose = true;
#endif
#if DEBUG
                verbose = true;
#endif
                Staging = UseProduction ? false : staging;
                OriginalVerbose = Verbose = verbose;
                AutoUpdateEnabled = autoUpdateEnabled;
                LockDown = lockDown;
                LockDownModSet = lockDownModSet;
                SelfUpdateSupported = selfUpdateSupported;
            }

            public bool Is64BitOperatingSystem { get; }
            bool UseProduction { get; }
            public bool Public { get; set; }
            public bool SkipExecutionConfirmation { get; }
            public bool IsInternal { get; }
            public bool IsStable { get; }
            public bool IgnoreErrorDialogs { get; }
            public bool SelfUpdateSupported { get; }
            /// <summary>
            ///     Startup parameters left over after processing low level startup parameters. Use FullStartupParameters if you need
            ///     all of them instead
            /// </summary>
            public string[] StartupParameters { get; }
            /// <summary>
            ///     All startup parameters, unfiltered.
            /// </summary>
            public string[] FullStartupParameters { get; }
            public string LockDownModSet { get; }
            public bool AutoUpdateEnabled { get; }
            public bool LockDown { get; }
            public bool Verbose { get; set; }
            public bool OriginalVerbose { get; }
            public bool Staging { get; }
            public bool UseLocalServer { get; set; }
            public bool ShuttingDown { get; set; }
            public bool UseElevatedService { get; set; }
            public bool Portable { get; }
        }
    }

    public interface IDomainEvent {}

    public interface IAsyncDomainEvent : IDomainEvent, IAsyncNotification {}

    public interface ISyncDomainEvent : IDomainEvent, INotification {}

    public abstract class DomainEvent<T> : IDomainEvent
    {
        protected DomainEvent(T subject) {
            Contract.Requires<ArgumentNullException>(subject != null);
            Subject = subject;
        }

        public T Subject { get; }
    }

    public abstract class SyncDomainEvent<T> : DomainEvent<T>, ISyncDomainEvent
    {
        protected SyncDomainEvent(T subject) : base(subject) {}
    }

    public abstract class AsyncDomainEvent<T> : DomainEvent<T>, IAsyncDomainEvent
    {
        protected AsyncDomainEvent(T subject) : base(subject) {}
    }
}
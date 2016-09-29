// <copyright company="SIX Networks GmbH" file="Generic.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Extensions;
using withSIX.Core.Logging;
using withSIX.Core.Services.Infrastructure;

namespace withSIX.Core
{
    public static partial class Tools
    {
        public static GenericTools Generic = new GenericTools();

        public class GenericTools : IEnableLogging
        {
            public const string TmpExtension = ".sixtmp";
            public static readonly Regex PathFilter = new Regex(@"(.*\\users\\)([^\\]+)(.*)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            static int _lastTicks = -1;
            static int _lastUtcTicks = -1;
            static DateTime _lastDateTime = DateTime.MinValue;
            static DateTime _lastUtcDateTime = DateTime.MinValue;
            public static readonly string DefaultDateFormat = "yyyy-MM-dd HH:mm";
            public virtual DateTime GetCurrentDateTime
            {
                get
                {
                    /*
                    var tickCount = Environment.TickCount;
                    if (tickCount == _lastTicks)
                        return _lastDateTime;
                        */
                    var dt = DateTime.Now;
                    //_lastTicks = tickCount;
                    //_lastDateTime = dt;
                    return dt;
                }
            }
            public virtual DateTime GetCurrentUtcDateTime
            {
                get
                {
                    /*
                    var tickCount = Environment.TickCount;
                    if (tickCount == _lastUtcTicks)
                        return _lastUtcDateTime;
                        */
                    var dt = DateTime.UtcNow;
                    //_lastUtcTicks = tickCount;
                    //_lastUtcDateTime = dt;
                    return dt;
                }
            }

            public string GetSmartDateString(DateTime dt) {
                var localTime = dt.ToLocalTime();
                return localTime.ToString(GetSmartDateFormat(localTime));
            }

            public string GetSmartDateFormat(DateTime localTime) {
                var currentTime = Generic.GetCurrentDateTime;
                var dayChange = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day);
                if (currentTime.Year == localTime.Year)
                    return localTime < dayChange ? "MM-dd HH:mm" : "HH:mm";

                return DefaultDateFormat;
            }

            public bool LongerAgoThan(DateTime lastTime, TimeSpan span) => GetCurrentUtcDateTime - lastTime > span;

            public string GetCombinedStartupParameters() => UacHelper.GetStartupParameters().CombineParameters();

            public virtual double NormalizeProgressValue(double progress) {
                if (progress < 0.0)
                    return 0.0;
                return progress > 100.0 ? 100.0 : Math.Round(progress, 2);
            }

            public virtual bool TryOpenUrl(string url) {
                if (string.IsNullOrWhiteSpace(url))
                    return false;

                try {
                    OpenUrl(url);
                    return true;
                } catch (Exception e) {
                    this.Logger().FormattedDebugException(e);
                }

                return false;
            }

            public int GetValidGameServerPort(int port) {
                if ((port < 1) || (port > IPEndPoint.MaxPort))
                    port = 2302;
                return port;
            }

            public virtual bool TryOpenUrl(Uri url) {
                try {
                    if (string.IsNullOrWhiteSpace(url.ToString()))
                        return false;
                    OpenUrl(url);
                    return true;
                } catch (Exception e) {
                    this.Logger().FormattedDebugException(e);
                }

                return false;
            }

            public void OpenUrl(string url) {
                url = url.Trim();
                //url = url.Replace("\"", String.Empty).Replace("\\", String.Empty);

                if (url[0] == '/')
                    url = Transfer.JoinUri(CommonUrls.ConnectUrl, url).ToString();

                try {
                    var uri = new Uri(url);
                    if (new[] {"http", "https", "ts3server", "mailto", "pws"}
                        .None(x => x == uri.Scheme.ToString())) {
                        throw new Exception("Illegal link! " + url);
                        return;
                    }
                    using (Process.Start(url)) {}
                } catch (Exception ex) {
                    throw new Exception($"Failed to open {url}", ex);
                }
            }

            public void OpenUrl(Uri uri) {
                Contract.Requires<ArgumentNullException>(uri != null);
                Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(uri.ToString()));

                OpenUrl(uri.ToString());
            }

            RegistryKey GetRegKey(string path, RegistryView bit = RegistryView.Registry32,
                RegistryHive hive = RegistryHive.LocalMachine) {
                Contract.Requires<ArgumentNullException>(path != null);
                Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(path));

                return OpenRegistry(bit, hive).OpenSubKey(path);
            }

            public T GetRegKeyValue<T>(string path, string value, RegistryView bit = RegistryView.Registry32,
                RegistryHive hive = RegistryHive.LocalMachine) {
                Contract.Requires<ArgumentNullException>(path != null);
                Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(path));
                Contract.Requires<ArgumentNullException>(value != null);

                using (var key = GetRegKey(path, bit, hive)) {
                    if (key == null)
                        return default(T);

                    return (T) key.GetValue(value);
                }
            }

            public T NullSafeGetRegKeyValue<T>(string path, string val,
                RegistryView view = RegistryView.Registry32,
                RegistryHive hive = RegistryHive.LocalMachine) {
                Contract.Requires<ArgumentNullException>(path != null);
                Contract.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(path));
                Contract.Requires<ArgumentNullException>(val != null);

                try {
                    return GetRegKeyValue<T>(path, val, view, hive);
                } catch (Exception e) {
                    this.Logger().FormattedDebugException(e);
                }
                return default(T);
            }

            public virtual RegistryKey OpenRegistry(RegistryView bit = RegistryView.Registry32,
                RegistryHive hive = RegistryHive.LocalMachine) => RegistryKey.OpenBaseKey(hive, bit);

            public void RunUpdater(params string[] args) {
                if (Common.Flags.UseElevatedService)
                    RunElevatedServiceOp(args);
                else
                    RunElevatedCommandlineOp(args);
            }

            static void RunElevatedCommandlineOp(params string[] args) {
                var parameters = args.CombineParameters();
                var outInfo =
                    ProcessManager.LaunchElevated(
                        new BasicLaunchInfo(
                            new ProcessStartInfo(Common.Paths.ServiceExePath.ToString(), parameters) {
                                CreateNoWindow = true
                            }) {
                            StartMinimized = true
                        });
                if (outInfo.ExitCode != 0) {
                    throw new UpdaterTaskException(string.Format("Failed to execute (code: {1}): {0}", parameters,
                        outInfo.ExitCode));
                }
            }

            static void RunElevatedServiceOp(params string[] args) {
                try {
                    var exitCode = _wcfClient.Value.PerformOperation(args);
                    if (exitCode != 0) {
                        throw new UpdaterServiceTaskException(string.Format("Failed to execute (code: {1}): {0}",
                            args.CombineParameters(), exitCode));
                    }
                } catch (Exception e) {
                    // FaultException
                    throw new UpdaterServiceTaskException(
                        $"Failed to execute: {args.CombineParameters()}\nInfo: {e.Message}");
                }
            }
        }
    }

    public class UpdaterTaskException : Exception
    {
        public UpdaterTaskException(string message) : base(message) {}
    }

    class UpdaterServiceTaskException : UpdaterTaskException
    {
        public UpdaterServiceTaskException(string message) : base(message) {}
    }
}
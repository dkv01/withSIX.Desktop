// <copyright company="SIX Networks GmbH" file="LowInitializer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Configuration;
using SN.withSIX.Core.Presentation.Bridge.Logging;
using Splat;
using ILogger = SN.withSIX.Core.Logging.ILogger;

namespace SN.withSIX.Core.Presentation.Bridge
{
    public class LowInitializer
    {
        public Func<EnvironmentSpecial.SpecialFolder, string> GetFolderPath { get; } = x => Environment.GetFolderPath(
            (Environment.SpecialFolder) Enum.Parse(typeof(Environment.SpecialFolder), x.ToString()));

        public void SetupLogging(string productTitle, string token = null) {
            if ((token != null) && (ConfigurationManager.AppSettings["Logentries.Token"] == null))
                ConfigurationManager.AppSettings["Logentries.Token"] = token;
            SetupNlog.Initialize(productTitle);
            if (Common.Flags.Verbose) {
                var splatLogger = new NLogSplatLogger();
                Locator.CurrentMutable.Register(() => splatLogger, typeof(ILogger));
            }
#if DEBUG
            LogHost.Default.Level = LogLevel.Debug;
#endif
        }
    }
}
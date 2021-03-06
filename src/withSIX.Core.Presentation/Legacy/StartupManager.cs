// <copyright company="SIX Networks GmbH" file="StartupManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Win32;
using withSIX.Core.Applications.Services;
using withSIX.Core.Infra.Cache;
using withSIX.Core.Logging;
using withSIX.Sync.Core.Legacy.SixSync;

namespace withSIX.Core.Presentation.Legacy
{
    public class StartupManager : IStartupManager, IEnableLogging
    {
        readonly ICacheManager _cacheManager;

        public StartupManager(ICacheManager cacheManager) {
            _cacheManager = cacheManager;
        }

        public async Task Exit() {
            try {
                await TryExit().ConfigureAwait(false);
            } catch (Exception e) {
                MainLog.Logger.FormattedErrorException(e,
                    "Error during Application shutdown. Usually these are swallowed.");
                throw;
            }
        }

        public virtual void RegisterServices() {
            Repository.InitializeConfig(
                PathConfiguration.GetRoamingRootPath().GetChildDirectoryWithName("Synq"));
        }

        public void RegisterUserAppKeys() {
            var key = Registry.CurrentUser.CreateSubKey(Common.AppCommon.ApplicationRegKey);
            key.SetValue("Path", Common.Paths.EntryLocation);
            key.SetValue("Version", CommonBase.AssemblyLoader.GetEntryVersion());
        }

        async Task TryExit() {
            await ExitI().ConfigureAwait(false);

            var tspan = Tools.Generic.GetCurrentUtcDateTime - Common.StartTime;
            var lifeTime = CalculateLifeSpan(tspan);
            MainLog.Logger.Info("Exiting application, lifetime: {0} ({1}s)", lifeTime, tspan.TotalSeconds);
        }

        protected virtual async Task ExitI() {
            // TODO: Or should we do this on startup, or even periodically?
            //await _cacheManager.Vacuum().ConfigureAwait(false);
            await _cacheManager.Shutdown().ConfigureAwait(false);
        }

        static string CalculateLifeSpan(TimeSpan tspan) {
            if (tspan.TotalDays >= 30)
                return "More than a month";
            if (tspan.TotalDays > 14)
                return "More than two weeks";
            if (tspan.TotalDays > 7)
                return "More than a week";
            if (tspan.TotalDays > 2)
                return "More than two days";
            if (tspan.TotalDays > 1)
                return "More than a day";
            if (tspan.TotalHours > 16)
                return "More than 16 hours";
            if (tspan.TotalHours > 8)
                return "More than 8 hours";
            if (tspan.TotalHours > 4)
                return "More than 4 hours";
            if (tspan.TotalHours > 2)
                return "More than 2 hours";
            if (tspan.TotalHours > 1)
                return "More than an hour";
            if (tspan.TotalMinutes > 30)
                return "More than 30 minutes";
            if (tspan.TotalMinutes > 10)
                return "More than 10 minutes";
            if (tspan.TotalMinutes > 5)
                return "More than 5 minutes";
            if (tspan.TotalMinutes > 1)
                return "More than a minute";
            return "Less than a minute";
        }
    }
}
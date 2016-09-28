// <copyright company="SIX Networks GmbH" file="BaseSteamCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Mini.Presentation.Core.Commands;
using SN.withSIX.Steam.Api;
using SN.withSIX.Steam.Api.Services;
using SN.withSIX.Steam.Core;

namespace SN.withSIX.Steam.Presentation.Commands
{
    public abstract class BaseSteamCommand : BaseCommandAsync
    {
        private readonly Lazy<App> _app;
        private readonly ISteamSessionFactory _factory;

        protected BaseSteamCommand(ISteamSessionFactory factory) {
            _factory = factory;
            _app = SystemExtensions.CreateLazy(() => new App(AppId));
            HasRequiredOption<uint>("a|appid=", "AppID", s => AppId = s);
            HasFlag("v|verbose", "Verbose", f => Common.Flags.Verbose = f);
        }

        public uint AppId { get; private set; }
        protected App App => _app.Value;

        protected Task DoWithSteamSession<T>(Func<Task<T>> act) => _factory.Do(AppId, SteamPathHelper.SteamPath, act);
        protected Task DoWithSteamSession(Func<Task> act) => DoWithSteamSession(() => act().Void());

        protected Tuple<PublishedFile, bool> ParsePid(string nfo) {
            ulong p;
            var force = false;
            if (nfo.StartsWith("!")) {
                force = true;
                p = Convert.ToUInt64(nfo.Substring(1));
            } else
                p = Convert.ToUInt64(nfo);
            return Tuple.Create(App.GetPf(p), force);
        }

        protected static void Progress(string msg) {
            Console.WriteLine(msg);
            if (Common.Flags.Verbose)
                MainLog.Logger.Info(msg);
            else
                MainLog.Logger.Debug(msg);
        }

        protected static void Info(string msg) {
            Console.WriteLine(msg);
            MainLog.Logger.Info(msg);
        }

        protected static void Error(string msg) {
            Console.WriteLine(msg);
            MainLog.Logger.Error(msg);
        }
    }
}
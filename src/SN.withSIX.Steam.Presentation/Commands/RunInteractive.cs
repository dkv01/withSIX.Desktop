// <copyright company="SIX Networks GmbH" file="RunInteractive.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using SN.withSIX.Steam.Api.Services;
using withSIX.Api.Models.Games;
using ISteamApi = withSIX.Steam.Plugin.Arma.ISteamApi;

namespace SN.withSIX.Steam.Presentation.Commands
{
    public class RunInteractive : BaseSteamCommand
    {
        private readonly ISteamSessionFactory _steamSessionFactory;

        public RunInteractive(ISteamSessionFactory steamSessionFactory) : base(steamSessionFactory) {
            IsCommand("interactive", "Run in interactive mode");
            _steamSessionFactory = steamSessionFactory;
        }

        public static ISteamApi SteamApi { get; private set; }

        protected override async Task<int> RunAsync(string[] remainingArguments) {
            if (AppId == (uint) SteamGameIds.Arma3) {
                await SteamActions.PerformArmaSteamAction(async api => {
                    SteamApi = api;
                    await RunWebsite().ConfigureAwait(false);
                }, (uint) SteamGameIds.Arma3, _steamSessionFactory).ConfigureAwait(false);
            } else {
                await DoWithSteamSession(RunWebsite).ConfigureAwait(false);
            }
            return 0;
        }

        private static async Task RunWebsite() {
            WebApp.Start<Startup>("http://127.0.0.66:48667");
            Console.WriteLine("Ready");
            await Task.Delay(-1).ConfigureAwait(false);
        }
    }
}
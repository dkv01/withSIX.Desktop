// <copyright company="SIX Networks GmbH" file="RunInteractive.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using withSIX.Api.Models.Games;
using withSIX.Steam.Api.Services;
using withSIX.Steam.Plugin.Arma;
using ISteamApi = withSIX.Steam.Plugin.Arma.ISteamApi;

namespace withSIX.Steam.Presentation.Commands
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
                    await RunWebsite(CancellationToken.None).ConfigureAwait(false);
                }, (uint) SteamGameIds.Arma3, _steamSessionFactory).ConfigureAwait(false);
            } else {
                await DoWithSteamSession(() => RunWebsite(CancellationToken.None)).ConfigureAwait(false);
            }
            return 0;
        }

        private static Task RunWebsite(CancellationToken ct)
            => new WebListener().Run(new IPEndPoint(IPAddress.Parse("127.0.0.66"), 48667), null, ct);
    }
}
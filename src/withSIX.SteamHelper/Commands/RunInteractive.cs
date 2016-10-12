﻿// <copyright company="SIX Networks GmbH" file="RunInteractive.cs">
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
using System.Linq;
using withSIX.Steam.Infra;

namespace withSIX.Steam.Presentation.Commands
{
    public class RunInteractive : BaseSteamCommand
    {
        private readonly IServiceMessenger _messenger;
        private readonly ISteamSessionFactory _steamSessionFactory;

        public RunInteractive(ISteamSessionFactory steamSessionFactory, IServiceMessenger messenger)
            : base(steamSessionFactory) {
            IsCommand("interactive", "Run in interactive mode");
            HasOption("b|bind=", "The address to listen on", s => Bind = s);
            _steamSessionFactory = steamSessionFactory;
            _messenger = messenger;
        }

        public string Bind { get; set; }

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

        private Task RunWebsite(CancellationToken ct) {
            var addr = GetIPEp(Bind ?? "127.0.0.1:0"); // default random port
            return new WebListener().Run(addr, null, ct);
        }

        private IPEndPoint GetIPEp(string str) {
            var split = str.Split(':');
            var ip = IPAddress.Parse(string.Join(":", split.Take(split.Length - 1)));
            var port = Convert.ToInt32(split.Last());
            return new IPEndPoint(ip, port);
        }
    }

    public interface IServiceMessenger {}
}
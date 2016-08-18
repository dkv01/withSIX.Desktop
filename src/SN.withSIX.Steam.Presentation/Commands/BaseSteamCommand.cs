// <copyright company="SIX Networks GmbH" file="BaseSteamCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using SN.withSIX.Mini.Presentation.Core.Commands;
using SN.withSIX.Steam.Api.Services;

namespace SN.withSIX.Steam.Presentation.Commands
{
    public abstract class BaseSteamCommand : BaseCommandAsync
    {
        private readonly ISteamSessionFactory _factory;

        protected BaseSteamCommand(ISteamSessionFactory factory) {
            _factory = factory;
            HasRequiredOption<uint>("a|appid=", "AppID", s => AppId = s);
        }

        public uint AppId { get; private set; }

        protected Task<SteamSession> StartSession() => _factory.Start(AppId);
    }
}
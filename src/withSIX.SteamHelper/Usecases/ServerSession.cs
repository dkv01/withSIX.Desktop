// <copyright company="SIX Networks GmbH" file="ServerSession.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameServerQuery;
using withSIX.Core;
using withSIX.Core.Applications.Services;
using withSIX.Steam.Plugin.Arma;
using withSIX.Steam.Presentation.Commands;
using withSIX.Steam.Presentation.Hubs;

namespace withSIX.Steam.Presentation.Usecases
{
    public abstract class ServerSession<TMessage> where TMessage : IRequireConnectionId, IHaveFilter, IRequireRequestId
    {
        private readonly IMessageBusProxy _mb;
        private readonly ISteamApi _steamApi;

        protected ServerSession(ISteamApi steamApi, IMessageBusProxy mb) {
            _steamApi = steamApi;
            _mb = mb;
        }

        protected TMessage Message { get; private set; }

        protected ServerFilterBuilder Builder { get; private set; }

        protected ServerBrowser Sb { get; private set; }

        protected CancellationToken Ct { get; private set; }

        protected void SendEvent<T>(T evt) => _mb.SendMessage(evt.ToClientEvent(Message));
        protected Task<ServerBrowser> CreateServerBrowser() => SteamActions.CreateServerBrowser(_steamApi);

        public async Task<BatchResult> Handle(TMessage message, CancellationToken ct) {
            Message = message;
            Builder = ServerFilterBuilder.FromValue(Message.Filter);
            using (var sb = await CreateServerBrowser().ConfigureAwait(false)) {
                using (var cts = new CancellationTokenSource()) {
                    ct.Register(cts.Cancel);
                    Ct = ct;
                    Sb = sb;
                    return await HandleInternal().ConfigureAwait(false);
                }
            }
        }

        protected abstract Task<BatchResult> HandleInternal();
    }


    public interface IHaveFilter
    {
        List<Tuple<string, string>> Filter { get; }
    }
}
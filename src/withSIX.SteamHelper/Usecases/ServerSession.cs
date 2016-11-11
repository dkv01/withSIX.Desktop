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
using withSIX.Steam.Api.Services;

namespace withSIX.Steam.Presentation.Usecases
{
    public abstract class ServerSession<TMessage> where TMessage : IHaveFilter
    {
        private readonly IMessageBusProxy _mb;
        private readonly IRequestScope _scope;

        protected ServerSession(IMessageBusProxy mb, IRequestScope scope, ISteamSessionLocator sessionLocator) {
            _mb = mb;
            _scope = scope;
            SessionLocator = sessionLocator;
        }

        protected TMessage Message { get; private set; }

        protected ISteamSessionLocator SessionLocator { get; }

        protected ServerFilterBuilder Builder { get; private set; }

        protected CancellationToken Ct { get; private set; }

        // TODO: Why not make this a bit more direct
        protected void SendEvent<T>(T evt) => _mb.SendMessage(Tuple.Create(_scope, evt));

        public Task<BatchResult> Handle(TMessage message, CancellationToken ct) {
            Message = message;
            Builder = ServerFilterBuilder.FromValue(Message.Filter);
            Ct = ct;
            return HandleInternal();
        }

        protected abstract Task<BatchResult> HandleInternal();
    }


    public interface IHaveFilter
    {
        List<Tuple<string, string>> Filter { get; }
    }
}
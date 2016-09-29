// <copyright company="SIX Networks GmbH" file="MessageBusProxy.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using ReactiveUI;
using withSIX.Core.Applications.Services;

namespace withSIX.Core.Presentation.Services
{
    public class MessageBusProxy : IMessageBusProxy, IPresentationService
    {
        private readonly IMessageBus _messageBus;

        public MessageBusProxy(IMessageBus messageBus) {
            _messageBus = messageBus;
        }

        public IObservable<T> Listen<T>() => _messageBus.Listen<T>();

        public void SendMessage<T>(T message, string contract = null) => _messageBus.SendMessage(message, contract);
    }
}
// <copyright company="SIX Networks GmbH" file="IMessageBusProxy.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace withSIX.Core.Applications.Services
{
    public interface IMessageBusProxy
    {
        IObservable<T> Listen<T>();
        void SendMessage<T>(T message, string contract = null);
    }
}
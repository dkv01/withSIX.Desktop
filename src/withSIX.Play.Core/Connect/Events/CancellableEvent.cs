// <copyright company="SIX Networks GmbH" file="CancellableEvent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Core;

namespace withSIX.Play.Core.Connect.Events
{
    public abstract class CancellableEvent : EventArgs, IAsyncDomainEvent
    {
        public bool Cancel { get; set; }
    }
}
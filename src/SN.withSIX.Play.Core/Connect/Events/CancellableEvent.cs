// <copyright company="SIX Networks GmbH" file="CancellableEvent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using SN.withSIX.Core;

namespace SN.withSIX.Play.Core.Connect.Events
{
    public abstract class CancellableEvent : EventArgs, IDomainEvent
    {
        public bool Cancel { get; set; }
    }
}
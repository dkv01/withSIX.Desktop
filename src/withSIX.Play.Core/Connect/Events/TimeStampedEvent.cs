// <copyright company="SIX Networks GmbH" file="TimeStampedEvent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace withSIX.Play.Core.Connect.Events
{
    public abstract class TimeStampedEvent : EventArgs, IAsyncDomainEvent
    {
        public DateTime TimeStamp { get; }

        protected TimeStampedEvent() {
            TimeStamp = Tools.Generic.GetCurrentUtcDateTime;
        }
    }
}
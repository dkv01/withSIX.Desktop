// <copyright company="SIX Networks GmbH" file="TimestampedDomainEvent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace withSIX.Core
{
    public abstract class TimestampedDomainEvent : ISyncDomainEvent, IHaveTimestamp
    {
        protected TimestampedDomainEvent() {
            // TODO: Or should this rather be set upon the actual saving? hmm
            Timestamp = Tools.Generic.GetCurrentUtcDateTime;
        }

        public DateTime Timestamp { get; }
    }
}
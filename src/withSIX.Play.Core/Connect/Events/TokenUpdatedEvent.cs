// <copyright company="SIX Networks GmbH" file="TokenUpdatedEvent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Core;
using withSIX.Api.Models.Premium;

namespace withSIX.Play.Core.Connect.Events
{
    public class TokenUpdatedEvent : IAsyncDomainEvent
    {
        public TokenUpdatedEvent(PremiumAccessTokenV1 premiumToken) {
            PremiumToken = premiumToken;
        }

        public PremiumAccessTokenV1 PremiumToken { get; }
    }
}
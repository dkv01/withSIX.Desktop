// <copyright company="SIX Networks GmbH" file="TokenUpdatedEvent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using SN.withSIX.Core;
using withSIX.Api.Models.Premium;

namespace SN.withSIX.Play.Core.Connect.Events
{
    public class TokenUpdatedEvent : IDomainEvent
    {
        public TokenUpdatedEvent(PremiumAccessTokenV1 premiumToken) {
            PremiumToken = premiumToken;
        }

        public PremiumAccessTokenV1 PremiumToken { get; }
    }
}
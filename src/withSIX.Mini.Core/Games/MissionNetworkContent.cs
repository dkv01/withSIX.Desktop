// <copyright company="SIX Networks GmbH" file="MissionNetworkContent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;

namespace withSIX.Mini.Core.Games
{
    [DataContract]
    public class MissionNetworkContent : NetworkContent
    {
        protected MissionNetworkContent() {}
        public MissionNetworkContent(string packageName, Guid gameId) : base(packageName, gameId) {}
        public override string ContentSlug { get; } = "missions";
    }
}
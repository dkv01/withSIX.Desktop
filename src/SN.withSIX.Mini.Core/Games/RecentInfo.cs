// <copyright company="SIX Networks GmbH" file="RecentInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;
using SN.withSIX.Core;

namespace SN.withSIX.Mini.Core.Games
{
    public interface IRecentInfo
    {
        DateTime CreatedAt { get; }
        DateTime LastUsed { get; }
        LaunchType LaunchType { get; }
    }

    [DataContract]
    public class RecentInfo : IRecentInfo
    {
        public RecentInfo(LaunchType launchType = LaunchType.Default) {
            LaunchType = launchType;
            CreatedAt = Tools.Generic.GetCurrentUtcDateTime;
            LastUsed = CreatedAt;
        }

        [DataMember]
        public DateTime CreatedAt { get; protected set; }
        [DataMember]
        public DateTime LastUsed { get; protected set; }
        [DataMember]
        public LaunchType LaunchType { get; protected set; }
    }
}
// <copyright company="SIX Networks GmbH" file="InstallInfo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;
using withSIX.Core;

namespace withSIX.Mini.Core.Games
{
    [DataContract]
    public class InstallInfo : IInstallInfo
    {
        protected InstallInfo() {}

        protected InstallInfo(long size, long sizePacked) : this() {
            Size = size;
            SizePacked = sizePacked;
            CreatedAt = Tools.Generic.GetCurrentUtcDateTime;
            LastInstalled = CreatedAt;
        }

        public InstallInfo(long size, long sizePacked, string version = null, bool completed = true)
            : this(size, sizePacked) {
            Version = version;
            Completed = completed;
        }

        [DataMember]
        public DateTime CreatedAt { get; protected set; }
        [DataMember]
        public DateTime LastInstalled { get; protected set; }
        [DataMember]
        public DateTime? LastUpdated { get; protected set; }
        [DataMember]
        public string Version { get; protected set; }
        [DataMember]
        public long Size { get; protected set; }
        [DataMember]
        public long SizePacked { get; protected set; }
        [DataMember]
        public bool Completed { get; protected set; } = true;

        internal void Updated(string version, long size, long sizePacked, bool completed) {
            Version = version;
            Size = size;
            SizePacked = sizePacked;
            Completed = completed;
            LastUpdated = Tools.Generic.GetCurrentUtcDateTime;
        }
    }

    public interface IInstallInfo {}
}
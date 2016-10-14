// <copyright company="SIX Networks GmbH" file="ServerKey.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Net;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace withSIX.Steam.Plugin.Arma
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ServerKey
    {
        public uint IpAddress { get; }
        public ushort Port { get; }

        public ServerKey(uint ipAddress, ushort port) {
            IpAddress = ipAddress;
            Port = port;
        }

        public bool Equals(ServerKey other) => (IpAddress == other.IpAddress) && (Port == other.Port);

        public override bool Equals([CanBeNull] object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            return obj is ServerKey && Equals((ServerKey) obj);
        }

        public override int GetHashCode() => (int) (IpAddress*0x18d) ^ Port.GetHashCode();

        public override string ToString() => string.Format("({0} {2}:{1})", IpAddress, Port, ToIpAddress());

        public IPAddress ToIpAddress() => new IPAddress(ReverseBytes(IpAddress));
        public IPEndPoint ToIpEndpoint() => new IPEndPoint(ToIpAddress(), Port);

        private static uint ReverseBytes(uint value) => (uint)
        (((value & 0xff) << 0x18) | ((value & 0xff00) << 8) | ((value & 0xff0000) >> 8) |
         ((value & -16777216) >> 0x18));
    }
}
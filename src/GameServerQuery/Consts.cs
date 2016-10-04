// <copyright company="SIX Networks GmbH" file="ServerQueryBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace GameServerQuery
{
    internal static class Consts
    {
        public const int DefaultTimeoutSeconds = 5;
        public const int DefaultSendTimeout = DefaultTimeoutSeconds*1000;
        public const int DefaultReceiveTimeout = DefaultTimeoutSeconds*1000;
    }
}
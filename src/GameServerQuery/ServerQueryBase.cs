// <copyright company="SIX Networks GmbH" file="ServerQueryBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace GameServerQuery
{
    public abstract class ServerQueryBase
    {
        const int DefaultTimeoutSeconds = 5;
        protected const int DefaultSendTimeout = DefaultTimeoutSeconds*1000;
        protected const int DefaultReceiveTimeout = DefaultTimeoutSeconds*1000;
    }
}
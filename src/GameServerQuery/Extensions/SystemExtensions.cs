// <copyright company="SIX Networks GmbH" file="SystemExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Net;

namespace GameServerQuery.Extensions
{
    public static class SystemExtensions
    {
        public static IPEndPoint ToIPEndPoint(this string address) {
            var split = address.Split(':');
            return new IPEndPoint(IPAddress.Parse(split[0]), int.Parse(split[1]));
        }
    }
}
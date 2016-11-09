// <copyright company="SIX Networks GmbH" file="ServerInfoModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Net;

namespace withSIX.Steam.Core.Services
{
    public class ServerInfoModel
    {
        public ServerInfoModel(IPEndPoint queryEndPoint) {
            QueryEndPoint = queryEndPoint;
            ConnectionEndPoint = QueryEndPoint;
        }

        public IPEndPoint ConnectionEndPoint { get; set; }
        public IPEndPoint QueryEndPoint { get; protected set; }

        public string Version { get; set; }
    }
}
// <copyright company="SIX Networks GmbH" file="ConnectCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using Newtonsoft.Json;
using withSIX.Api.Models.Extensions;

namespace withSIX.Play.Core.Games.Legacy.Arma.Commands
{
    [DataContract]
    public class ConnectCommand : ISendMessage
    {
        public static string Command = "connect";

        public ConnectCommand(ServerAddress address, string password = null) {
            Ip = address.IP.ToString();
            Port = address.Port;
            Password = password;
        }

        [DataMember, JsonProperty("ip")]
        public string Ip { get; set; }
        [DataMember, JsonProperty("port")]
        public int Port { get; set; }
        [DataMember, JsonProperty("password")]
        public string Password { get; set; }

        public string ToGameCommand() => Command + " " + this.ToJson();
    }
}
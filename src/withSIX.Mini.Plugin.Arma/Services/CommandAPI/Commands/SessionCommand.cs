// <copyright company="SIX Networks GmbH" file="SessionCommand.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Api.Models.Extensions;
using withSIX.Core.Extensions;

namespace withSIX.Mini.Plugin.Arma.Services.CommandAPI.Commands
{
    public class SessionCommand : ISendReceiveMessage
    {
        public static string Command = "session";
        public string PlayerId { get; set; }
        public bool Hosting { get; set; }
        public string Island { get; set; }
        public string Mission { get; set; }
        public string Host { get; set; }
        public string HostIP { get; set; }

        public string ToGameCommand() => Command;

        public void ParseInput(string substring) {
            var data = substring.FromJson<SessionCommand>();
            data.CopyProperties(this);
        }
    }
}
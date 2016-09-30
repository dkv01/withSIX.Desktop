// <copyright company="SIX Networks GmbH" file="SixRepoApp.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;

namespace withSIX.Play.Core.Games.Legacy.Repo
{
    public enum AppType
    {
        Teamspeak3,
        Mumble
    }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Sync.Core.Models.Repositories.SixSync"
        )]
    public class SixRepoApp : IBaseYaml, IHaveType<AppType>
    {
        [DataMember]
        public string Channel { get; set; }
        [DataMember]
        public string ChannelPassword { get; set; }
        [DataMember]
        public string Name { get; set; }
        public string Address
        {
            get
            {
                switch (Type) {
                case AppType.Teamspeak3: {
                    // #ts3server://ts3.hoster.com?port=9987&nickname=UserNickname&password=serverPassword&channel=MyDefaultChannel&channelpassword=defaultChannelPassword&token=TokenKey&addbookmark=1
                    var baseUrl = $"ts3server://{Ip}?port={(Port > 0 ? Port : 9898)}";
                    if (!string.IsNullOrWhiteSpace(Password))
                        baseUrl += "&password=" + Password;
                    if (!string.IsNullOrWhiteSpace(Channel))
                        baseUrl += "&channel=" + Channel;
                    if (!string.IsNullOrWhiteSpace(ChannelPassword))
                        baseUrl += "&channelpassword=" + ChannelPassword;
                    return baseUrl;
                }
                default: {
                    return null;
                }
                }
            }
        }
        [DataMember]
        public bool IsHidden { get; set; }
        [DataMember]
        public string Ip { get; set; }
        [DataMember]
        public int Port { get; set; }
        [DataMember]
        public string Password { get; set; }

        public string ToYaml() {
            throw new NotImplementedException();
        }

        [DataMember]
        public AppType Type { get; set; } = AppType.Teamspeak3;

        public string GetDisplayName() => $"{Type}: {Name}";
    }
}
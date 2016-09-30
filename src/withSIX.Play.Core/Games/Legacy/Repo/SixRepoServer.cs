// <copyright company="SIX Networks GmbH" file="SixRepoServer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using withSIX.Play.Core.Games.Entities;
using withSIX.Sync.Core;
using withSIX.Sync.Core.Legacy;
using withSIX.Api.Models.Games;

namespace withSIX.Play.Core.Games.Legacy.Repo
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Sync.Core.Models.Repositories.SixSync"
        )]
    public class SixRepoServer : IBaseYaml
    {
        static readonly Dictionary<string, string> mapping = new Dictionary<string, string> {
            {"a3", GameIds.Arma3},
            {"a2", GameIds.Arma2Co},
            {"a2co", GameIds.Arma2Co},
            {"arma3", GameIds.Arma3},
            {"arma2oaco", GameIds.Arma2Co},
            {"arma2", GameIds.Arma2},
            {"arma2co", GameIds.Arma2Co},
            {"arma2oa", GameIds.Arma2Oa},
            {"toh", GameIds.TKOH}
        };
        public static readonly string[] SYS = {":modlist", ":sixmodlist"};
        ServerAddress _address;
        string[] _allowedMods;
        string _ip;
        int _port;
        string[] _requiredMods;

        public SixRepoServer() {
            Motd = new string[0];
            Rules = new string[0];
            RequiredMods = new string[0];
            AllowedMods = new string[0];
            Apps = new string[0];
            Missions = new string[0];
            MPMissions = new string[0];
        }

        public ServerAddress Address
        {
            get { return _address ?? (_address = GetAddy()); }
            private set { _address = value; }
        }

        public ServerAddress GetQueryAddress() {
            var a = Address;
            if (a == null)
                return null;
            return Address.ToQueryAddress();
        }
        [DataMember]
        public string Game { get; set; }
        [DataMember]
        public string Image { get; set; }
        [DataMember]
        public string ImageLarge { get; set; }
        public Server Server { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Uuid { get; set; }
        [DataMember]
        public string Ip
        {
            get { return _ip; }
            set
            {
                _ip = value;
                Address = null;
            }
        }
        [DataMember]
        public int Port
        {
            get { return _port; }
            set
            {
                _port = value;
                Address = null;
            }
        }
        [DataMember]
        public string Info { get; set; }
        [DataMember]
        public string Password { get; set; }
        [DataMember]
        public string[] Motd { get; set; }
        [DataMember]
        public string[] Rules { get; set; }
        [DataMember]
        public string[] RequiredMods
        {
            get { return _requiredMods; }
            set
            {
                Validate(value);
                _requiredMods = value;
            }
        }
        [DataMember]
        public string[] AllowedMods
        {
            get { return _allowedMods; }
            set
            {
                Validate(value);
                _allowedMods = value;
            }
        }
        [DataMember]
        public string[] Apps { get; set; }
        [DataMember]
        public string[] Missions { get; set; }
        [DataMember]
        public string[] MPMissions { get; set; }
        [DataMember]
        public bool IsOpen { get; set; }
        [DataMember]
        public bool IsHidden { get; set; }
        [DataMember]
        public bool ForceModUpdate { get; set; }
        [DataMember]
        public bool ForceServerName { get; set; }

        public string ToYaml() {
            var graph = new Dictionary<string, object> {
                {":name", Name},
                {":ip", Ip},
                {":port", Port},
                {":uuid", Uuid},
                {":game", Game},
                {":force_server_name", ForceServerName},
                {":open", IsOpen},
                {":hidden", IsHidden},
                {":force_mod_update", ForceModUpdate},
                {":info", Info},
                {":motd", Motd},
                {":password", Password},
                {":rules", Rules},
                {":image", Image},
                {":image_large", ImageLarge},
                {":required_mods", RequiredMods},
                {":allowed_mods", AllowedMods},
                {":apps", Apps},
                {":missions", Missions},
                {":mpmissions", MPMissions}
            };
            return SyncEvilGlobal.Yaml.ToYaml(graph);
        }

        public Guid GetGameUuid() {
            var game = Game;
            if (string.IsNullOrWhiteSpace(game))
                return Guid.Empty;

            var key = game.ToLower();
            return new Guid(mapping.ContainsKey(key) ? mapping[key] : game);
        }

        ServerAddress GetAddy() => SAStuff.GetAddy($"{_ip}:{(_port == 0 ? 2302 : _port)}");

        static void Validate(IEnumerable<string> concat) {
            if (concat == null)
                return;

            foreach (var i in concat.Where(x => !SYS.Contains(x)))
                withSIX.Core.Validators.PathValidator.ValidateName(i);
        }

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context) {
            if (Motd == null)
                Motd = new string[0];
            if (Rules == null)
                Rules = new string[0];
            if (RequiredMods == null)
                RequiredMods = new string[0];
            if (AllowedMods == null)
                AllowedMods = new string[0];
            if (Apps == null)
                Apps = new string[0];
            if (Missions == null)
                Missions = new string[0];
            if (MPMissions == null)
                MPMissions = new string[0];
        }
    }
}
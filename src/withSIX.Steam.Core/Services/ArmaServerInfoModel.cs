using System.Collections.Generic;
using System.Net;
using GameServerQuery.Games.RV;
using withSIX.Api.Models.Servers.RV;
using ServerModInfo = GameServerQuery.Games.RV.ServerModInfo;

namespace withSIX.Steam.Core.Services
{
    public class ArmaServerInfoModel : ServerInfoModel
    {
        public ArmaServerInfoModel(IPEndPoint queryEndPoint) : base(queryEndPoint) {
            ModList = new List<ServerModInfo>();
            SignatureList = new HashSet<string>();
        }

        public AiLevel AiLevel { get; set; }

        public int CurrentPlayers { get; set; }

        public Difficulty Difficulty { get; set; }

        public Dlcs DownloadableContent { get; set; }

        public GameTags GameTags { get; set; }

        public HelicopterFlightModel HelicopterFlightModel { get; set; }

        public bool IsModListOverflowed { get; set; }

        public bool IsSignatureListOverflowed { get; set; }

        public bool IsThirdPersonViewEnabled { get; set; }

        public bool IsVacEnabled { get; set; }

        public bool IsWeaponCrosshairEnabled { get; set; }

        public string Map { get; set; }

        public int MaxPlayers { get; set; }

        public string Mission { get; set; }

        public List<ServerModInfo> ModList { get; set; }

        public string Name { get; set; }

        public int Ping { get; set; }

        public bool RequirePassword { get; set; }

        public bool RequiresExpansionTerrain { get; set; }

        public int ServerVersion { get; set; }

        public HashSet<string> SignatureList { get; set; }

        public string Tags { get; set; }

        public bool ReceivedRules { get; set; }
    }
}
// <copyright company="SIX Networks GmbH" file="Arma3Game.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GameServerQuery;
using GameServerQuery.Parsers;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Attributes;
using SN.withSIX.Mini.Plugin.Arma.Attributes;
using withSIX.Api.Models.Games;
using Player = SN.withSIX.Mini.Core.Games.Player;

namespace SN.withSIX.Mini.Plugin.Arma.Models
{
    [Game(GameIds.Arma3, Name = "Arma 3", Slug = "Arma-3",
        Executables = new[] {"arma3.exe"},
        IsPublic = true,
        ServerExecutables = new[] {"arma3server.exe"},
        LaunchTypes = new[] {LaunchType.Singleplayer, LaunchType.Multiplayer},
        Dlcs = new[] {"Karts", "Helicopters", "Marksmen"})]
    [SteamInfo(SteamGameIds.Arma3, "Arma 3", DRM = true)]
    [RegistryInfo(BohemiaRegistry + @"\ArmA 3", "main")]
    [RvProfileInfo("Arma 3", "Arma 3 - other profiles",
        "Arma3Profile")]
    [SynqRemoteInfo("1ba63c97-2a18-42a7-8380-70886067582e", "82f4b3b2-ea74-4a7c-859a-20b425caeadb" /*GameUUids.Arma3 */)
    ]
    [DataContract]
    public class Arma3Game : Arma2OaGame, IQueryServers
    {
        const string BattleEyeExe = "arma3battleye.exe";
        public static readonly string[] Arma2TerrainPacks = {
            "@A3Mp", "@AllInArmaTerrainPack",
            "@AllInArmaTerrainPackLite", "@cup_terrains_core"
        };
        private static readonly SourceQueryParser sourceQueryParser = new SourceQueryParser();
        private static readonly string[] getCompatibilityMods = {"@AllInArmaStandaloneLite"};
        private static readonly string[] getCompatibilityTerrains = {"@cup_terrains_core"};

        static readonly string[] emptyAddons = new string[0];
        readonly string[] _a3MpCategories = {"Island", "Objects (Buildings, Foliage, Trees etc)"};
        private readonly Guid[] _getCompatibleGameIds;
        readonly string[] _objectCategories = {"Objects (Buildings, Foliage, Trees etc)"};
        readonly Arma3GameSettings _settings;
        protected Arma3Game(Guid id) : this(id, new Arma3GameSettings()) {}

        public Arma3Game(Guid id, Arma3GameSettings settings) : base(id, settings) {
            _settings = settings;
            _getCompatibleGameIds = new[] {Id, GameGuids.Arma2Co};
        }

        protected override string[] BeGameParam { get; } = {"2", "1"};

        public async Task<List<IPEndPoint>> GetServers() {
            var master = new SourceMasterQuery("arma3");
            var r = await master.GetParsedServers().ConfigureAwait(false);
            return r.Select(x => x.Address).ToList();
        }

        public async Task<List<ServerInfo>> GetServerInfos(IReadOnlyCollection<IPEndPoint> addresses,
            bool inclPlayers = false) {
            var infos = new List<ServerInfo>();
            // TODO: Use serverquery queue ?
            foreach (var a in addresses) {
                var serverInfo = new ServerInfo {Address = a};
                infos.Add(serverInfo);
                var server = new Server(serverInfo);
                using (
                    var serverQueryState = new ServerQueryState(server, sourceQueryParser) {HandlePlayers = inclPlayers}
                    ) {
                    var q = new SourceServerQuery(serverQueryState, "arma3");
                    await q.UpdateAsync().ConfigureAwait(false);
                    try {
                        serverQueryState.UpdateServer();
                    } catch (Exception ex) {
                        MainLog.Logger.FormattedWarnException(ex, "While processing server " + serverInfo.Address);
                    }
                }
            }
            return infos;
        }

        protected override InstallContentAction GetInstallAction(
            IDownloadContentAction<IInstallableContent> action) {
            var content = action.Content;
            return new InstallContentAction(HandleAia(content), action.CancelToken) {
                RemoteInfo = RemoteInfo,
                Paths = ContentPaths,
                Game = this,
                Cleaning = ContentCleaning,
                Force = action.Force,
                HideLaunchAction = action.HideLaunchAction,
                Name = action.Name
            };
        }

        private IReadOnlyCollection<IContentSpec<IInstallableContent>> HandleAia(
            IReadOnlyCollection<IContentSpec<IInstallableContent>> content) {
            var info = new AiaInfo(content.Select(x => x.Content).OfType<IHavePackageName>());
            var newModsList = content.ToList();
            if (info.HasAia() && info.HasCup()) {
                //    if (aiaSpecific != null || aiaSpecificLite != null)
                //      newModsList.Remove(Cup);
                //                else
                newModsList.RemoveAll(x => info.IsAia(x.Content));
            }
            if (info.HasCup() || info.HasAia())
                newModsList.RemoveAll(x => info.IsA3Mp(x.Content));
            return newModsList;
        }

        private IReadOnlyCollection<ILaunchableContent> HandleAia(IReadOnlyCollection<ILaunchableContent> content) {
            var info = new AiaInfo(content.OfType<IHavePackageName>());
            var newModsList = content.ToList();
            if (info.HasAia() && info.HasCup()) {
                //    if (aiaSpecific != null || aiaSpecificLite != null)
                //      newModsList.Remove(Cup);
                //                else
                newModsList.RemoveAll(info.IsAia);
            }
            if (info.HasCup() || info.HasAia())
                newModsList.RemoveAll(info.IsA3Mp);
            return newModsList;
        }

        protected override IReadOnlyCollection<ILaunchableContent> GetLaunchables(ILaunchContentAction<IContent> action)
            => HandleAia(base.GetLaunchables(action));

        protected override StartupBuilder GetStartupBuilder() => new StartupBuilder(this, new Arma3ModListBuilder());

        protected override IAbsoluteFilePath GetBattleEyeClientExectuable()
            => GetExecutable().GetBrotherFileWithName(BattleEyeExe);

        public override IReadOnlyCollection<Guid> GetCompatibleGameIds() => _getCompatibleGameIds;

        public override IReadOnlyCollection<string> GetCompatibilityMods(string packageName,
            IReadOnlyCollection<string> tags)
            => tags.Any(x => _objectCategories.ContainsIgnoreCase(x)) ? emptyAddons : TerrainsVsOther(tags);

        private string[] TerrainsVsOther(IEnumerable<string> tags)
            => tags.Any(x => _a3MpCategories.ContainsIgnoreCase(x)) ? getCompatibilityTerrains : getCompatibilityMods;

        class AiaInfo
        {
            public AiaInfo(IEnumerable<IHavePackageName> contentWithPackageNames) {
                const string allinarmaTp = "@AllInArmaTerrainPack";
                const string allinarmaTpLite = "@AllInArmaTerrainPackLite";
                const string cupTerrainCore = "@cup_terrains_core";
                const string cupTerrainMaps = "@cup_terrains_maps";
                const string a3mappack = "@A3Mp";
                Aia = contentWithPackageNames.FirstOrDefault(x => Matches(x.PackageName, allinarmaTp));
                A3Mp = contentWithPackageNames.FirstOrDefault(x => Matches(x.PackageName, a3mappack));
                AiaLite = contentWithPackageNames.FirstOrDefault(x => Matches(x.PackageName, allinarmaTpLite));
                Cup = contentWithPackageNames.FirstOrDefault(x => Matches(x.PackageName, cupTerrainCore));
                CupMaps = contentWithPackageNames.FirstOrDefault(x => Matches(x.PackageName, cupTerrainMaps));
                //var aiaSpecific = enabledMods.Contains(Aia) ? Aia : null;
                //var aiaSpecificLite = enabledMods.Contains(AiaLite) ? AiaLite : null;
            }

            public IHavePackageName Aia { get; }
            public IHavePackageName AiaLite { get; }
            public IHavePackageName A3Mp { get; }
            public IHavePackageName Cup { get; }
            public IHavePackageName CupMaps { get; }

            static bool Matches(string packageName, string modName)
                => packageName.Equals(modName, StringComparison.CurrentCultureIgnoreCase);

            public bool HasAia() => Aia != null || AiaLite != null;
            public bool HasCup() => Cup != null;

            public bool IsCup(IContent content) => Cup == content || CupMaps == content;
            public bool IsAia(IContent content) => Aia == content || AiaLite == content;
            public bool IsA3Mp(IContent content) => A3Mp == content;
        }

        public class AllInArmaGames
        {
            readonly Game[] _games;
            readonly Game[] _gamesSupportModding;
            internal readonly Arma1Game Arma1;
            internal readonly Arma2Game Arma2;
            //internal readonly Arma2FreeGame Arma2Free;
            internal readonly Arma2OaGame Arma2Oa;
            internal readonly TakeOnHelicoptersGame TakeOn;

            public AllInArmaGames(Arma1Game arma1Game, Arma2Game arma2Game, //Arma2FreeGame arma2FreeGame,
                Arma2OaGame arma2OaGame,
                TakeOnHelicoptersGame takeOnHelicoptersGame) {
                Arma1 = arma1Game;
                Arma2 = arma2Game;
                //Arma2Free = arma2FreeGame;
                Arma2Oa = arma2OaGame;
                TakeOn = takeOnHelicoptersGame;
                _games = new Game[] {arma1Game, arma2Game, arma2OaGame, takeOnHelicoptersGame}; // arma2FreeGame, 
                _gamesSupportModding = new Game[] {arma1Game, arma2Game, arma2OaGame, takeOnHelicoptersGame};
                //_games.OfType<ISupportModding>().ToArray();
            }

            internal Game Find(Guid id) => _games.First(x => x.Id == id);
        }

        protected class Arma3ModListBuilder : ModListBuilder
        {
            const string AllInArmaA1Dummies = "A1Dummies";
            static readonly string[] aiaStandaloneMods = {
                "@AllInArmaStandaloneLite", "@AllInArmaStandalone", "@IFA3SA",
                "@IFA3SA_Lite"
            };
            //static readonly string[] aiamods = {"@AllInArma"};
            static readonly string[] ifModFolders = {"@LIB_DLC_1", "@IF_Other_Addons", "@IF"};
            static readonly string[] ifMainModFolders = {"@IF", "@IFA3", "@IFA3M"};
            static readonly string[] ifModFoldersLite = ifModFolders.Select(x => x + "_Lite").ToArray();
            static readonly string[] ifMainModFoldersLite = ifMainModFolders.Select(x => x + "_Lite").ToArray();
            //readonly AllInArmaGames _aiaGames;
            List<IAbsoluteDirectoryPath> _additional;
            List<IAbsoluteDirectoryPath> _additionalAiA;
            IfaState _ifa;
            IReadOnlyCollection<IModContent> _nonAiaMods;
            // we dont need to support legacy AiA anymore with standalone available..
            /*
            public Arma3ModListBuilder(AllInArmaGames aiaGames)
            {
                _aiaGames = aiaGames;
            }*/

            protected override void ProcessMods() {
                _additional = new List<IAbsoluteDirectoryPath>();
                _additionalAiA = new List<IAbsoluteDirectoryPath>();
                _ifa = IfaState.None;
                _nonAiaMods = InputMods.Where(x => !IsAiaSAMod(x)).ToArray(); // !IsAiaMod(x) && 
                ProcessIronFrontMods();
                AddPrimaryGameFolders();
                ProcessAndAddNormalModsWithAdditionalGameRequirements();
                AddNormalMods();
                ProcessAndAddAiaMods();
            }

            void AddNormalMods() {
                HandleA3Mp();
                OutputMods.AddRange(_nonAiaMods.SelectMany(GetModPaths));
            }

            void HandleA3Mp() {
                foreach (var a3Mp in Arma2TerrainPacks.Select(m => InputMods
                    .FirstOrDefault(x => x.PackageName.Equals(m, StringComparison.OrdinalIgnoreCase)))
                    .Where(a3Mp => a3Mp != null))
                    OutputMods.AddRange(GetModPaths(a3Mp));
            }

            void ProcessAndAddNormalModsWithAdditionalGameRequirements() {
                //foreach (var mod in _nonAiaMods)
                //  ProcessModIfHasAdditionalGameRequirements(mod);
                OutputMods.AddRange(_additional);
            }

            void ProcessAndAddAiaMods() {
                //foreach (var mod in InputMods.Where(IsAiaMod))
                //ProcessModIfHasAdditionalGameRequirements(mod);

                foreach (var mod in InputMods.Where(IsAiaSAMod))
                    ProcessAiaLiteMod(mod);

                // problematic?
                //OutputMods.RemoveAll(x => x..Split('/', '\\').First().Equals("@allinarma", StringComparison.CurrentCultureIgnoreCase));
                OutputMods.RemoveAll(x => _additionalAiA.Contains(x));
                OutputMods.AddRange(_additionalAiA);
            }

            void ProcessAiaLiteMod(IModContent mod) {
                _additionalAiA.AddRange(GetModPaths(mod));
                if (_ifa > IfaState.None)
                    HandleIfa();
            }

            /*
        void ProcessModIfHasAdditionalGameRequirements(IModContent mod) {
            var requirements = mod.GetGameRequirements().Select(_aiaGames.Find).ToArray();
            if (!requirements.Any())
                return;

            var existingGames = requirements.Where(x => x.InstalledState.IsInstalled);
            if (!aiamods.ContainsIgnoreCase(mod.PackageName))
                ProcessNonAiaMods(mod, existingGames);
            else
                HandleAiaMods(mod, existingGames);
        }
                
            static bool IsAiaMod(IModContent x) {
                return aiamods.ContainsIgnoreCase(x.PackageName);
            }
                */

            static bool IsAiaSAMod(IModContent x) => aiaStandaloneMods.ContainsIgnoreCase(x.PackageName);

            void ProcessIronFrontMods() {
                if (InputMods.Any(x => ifMainModFolders.ContainsIgnoreCase(x.PackageName)))
                    _ifa = IfaState.Full;
                else if (InputMods.Any(x => ifMainModFoldersLite.ContainsIgnoreCase(x.PackageName)))
                    _ifa = IfaState.Lite;
                else
                    return;
                InputMods.RemoveAll(IsIronFrontFullOrLiteMod);
                var validGamePaths = GetValidGamePaths();
                OutputMods.AddRange(ExistingMods(validGamePaths, _ifa == IfaState.Lite ? ifModFoldersLite : ifModFolders));
                OutputMods.AddRange(ExistingMods(validGamePaths,
                    _ifa == IfaState.Lite ? ifMainModFoldersLite : ifMainModFolders));
            }

            static bool IsIronFrontFullOrLiteMod(IModContent x) => ifMainModFolders.ContainsIgnoreCase(x.PackageName) ||
                                                                   ifMainModFoldersLite.ContainsIgnoreCase(x.PackageName) ||
                                                                   ifModFolders.ContainsIgnoreCase(x.PackageName)
                                                                   || ifModFoldersLite.ContainsIgnoreCase(x.PackageName);

            static IEnumerable<IAbsoluteDirectoryPath> ExistingMods(IAbsoluteDirectoryPath[] paths, params string[] mods)
                => paths.Any()
                    ? mods.Select(
                        x => paths.Select(path => path.GetChildDirectoryWithName(x)).FirstOrDefault(p => p.Exists))
                        .Where(x => x != null)
                    : Enumerable.Empty<IAbsoluteDirectoryPath>();

            /*
            void ProcessNonAiaMods(IModContent mod, IEnumerable<Game> existingGames) {
                foreach (var g in existingGames)
                    AddGameDefaultAndAdditionalModFolders(g, _additional);

                _additional.Add(Spec.GamePath);
                _additional.AddRange(GetModPaths(mod));
            }

            static void AddGameDefaultAndAdditionalModFolders(Game game, List<IAbsoluteDirectoryPath> modList) {
                modList.Add(game.InstalledState.Directory);
                var md = game as ISupportModding;
                if (md != null)
                  modList.AddRange(md.GetAdditionalLaunchMods().OfType<IAbsoluteDirectoryPath>());
            }

            void HandleAiaMods(IModContent mod, IEnumerable<Game> existingGames) {
                //var aiaModPath = mod.Controller.Path;
                _additionalAiA.Add(CombineModPathIfNeeded(SubMod(mod, "ProductDummies")));

                //HandleGames(existingGames, aiaModPath);

                _additionalAiA.Add(Spec.GamePath);
                _additionalAiA.Add(CombineModPathIfNeeded(SubMod(mod, "Core")));

                if (_ifa > IfaState.None)
                    HandleIfa();

                _additionalAiA.Add(CombineModPathIfNeeded(SubMod(mod, "PostA3")));
            }
            */

            void HandleIfa() {
                var gamePaths = GetValidGamePaths();
                _additionalAiA.AddRange(ExistingMods(gamePaths, GetIFAMod("@IFA3M")));
                _additionalAiA.AddRange(ExistingMods(gamePaths, GetIFAMod("@IFA3")));
            }

            string GetIFAMod(string mod) {
                var lite = _ifa == IfaState.Lite ? "_Lite" : "";
                return mod + lite;
            }

            /*
            void HandleGames(IEnumerable<Game> existingGames, IAbsoluteDirectoryPath aiaModPath) {
                var list = existingGames.ToList();
                HandleArma1(list, aiaModPath);
                foreach (var g in list)
                    AddGameDefaultAndAdditionalModFolders(g, _additionalAiA);
            }

            void HandleArma1(ICollection<Game> existingGames, IAbsoluteDirectoryPath aiaModPath) {
                var a1 = existingGames.FirstOrDefault(g => g.Id == GameUuids.Arma1);
                if (a1 == null)
                    return;
                existingGames.Remove(a1);
                AddGameDefaultAndAdditionalModFolders(a1, _additionalAiA);
                _additionalAiA.Add(aiaModPath.GetChildDirectoryWithName(AllInArmaA1Dummies));
            }
            */

            IAbsoluteDirectoryPath[] GetValidGamePaths()
                => new[] {Spec.ModPath, Spec.GamePath}.Where(x => x != null).ToArray();

            enum IfaState
            {
                None,
                Lite,
                Full
            }
        }
    }

    public class Server : IServer
    {
        static readonly ConcurrentDictionary<string, Regex> rxCache = new ConcurrentDictionary<string, Regex>();

        public Server(ServerInfo info) {
            Info = info;
            Address = info.Address;
        }

        public ServerInfo Info { get; }
        public bool IsUpdating { get; set; }

        public void UpdateStatus(Status status) {
            Info.Status = (int) status;
        }

        public void UpdateInfoFromResult(ServerQueryResult result) {
            Info.Ping = result.Ping;
            Info.Name = result.GetSettingOrDefault("name");
            Info.MissionName = result.GetSettingOrDefault("game");
            Info.MapName = result.GetSettingOrDefault("map");
            Info.NumPlayers = result.GetSettingOrDefault("playerCount").TryInt();
            Info.MaxPlayers = result.GetSettingOrDefault("playerMax").TryInt();
            var port = result.GetSettingOrDefault("port").TryInt();
            if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
                port = Info.Address.Port - 1;
            Info.ServerAddress = new IPEndPoint(Info.Address.Address, port);
            Info.Mods = GetList(result.Settings, "modNames").ToList();
            Info.PasswordRequired = result.GetSettingOrDefault("visibility").TryInt() > 0;
            Info.GameVersion = GetVersion(result.GetSettingOrDefault("version"));
            var tags = result.GetSettingOrDefault("keywords");
            if (tags != null)
                new SourceTagParser(tags, Info).HandleTags();
            Info.Players =
                result.Players.OfType<SourcePlayer>()
                    .Select(x => new Player {Name = x.Name, Score = x.Score, Duration = x.Duration})
                    .ToList();
        }

        public IPEndPoint Address { get; }

        static Version GetVersion(string version) => version?.TryParseVersion();

        static IEnumerable<string> GetList(IEnumerable<KeyValuePair<string, string>> dict, string keyWord) {
            var rx = GetRx(keyWord);
            return string.Join("", (from kvp in dict.Where(x => x.Key.StartsWith(keyWord))
                let w = rx.Match(kvp.Key)
                where w.Success
                select new {Index = w.Groups[1].Value.TryInt(), Total = w.Groups[2].Value.TryInt(), kvp.Value})
                .OrderBy(x => x.Index).SelectMany(x => x.Value))
                .Split(';')
                .Where(x => !string.IsNullOrWhiteSpace(x));
        }

        static Regex GetRx(string keyWord) {
            Regex rx;
            if (rxCache.TryGetValue(keyWord, out rx))
                return rx;
            return
                rxCache[keyWord] =
                    new Regex(@"^" + keyWord + @":([0-9]+)\-([0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        class SourceTagParser
        {
            /*
Value in the string	 identifier	 value	 meaning
bt,	 b	 true	 BattleEye 
r120,	 r	 1.20	 RequiredVersion
n0,	 n	 0	 RequiredBuildNo
s1,	 s	 1	 ServerState
i2,	 i	 2	 Difficulty
mf,	 m	 false	 EqualModRequired
lf,	 l	 false	 Lock
vt,	 v	 true	 VerifySignatures
dt,	 d	 true	 Dedicated
ttdm	 t	 tdm	 GameType
g65545,	 g	 65545	 Language
c0-52,	 c	 long.=0 lat.=52	 LongLat
pw	 p	 Windows	 Platform
Example
gameTags = bt,r120,n0,s1,i2,mf,lf,vt,dt,ttdm,g65545,c0-52,pw,
*/
            readonly ServerInfo _server;
            readonly Dictionary<string, string> _settings;

            public SourceTagParser(string tags, ServerInfo server) {
                _settings = new Dictionary<string, string>();
                foreach (var t in tags.Split(',')) {
                    var key = JoinChar(t.Take(1));
                    _settings.Add(key, JoinChar(t.Skip(1)));
                    // TODO: HACK: workaround t game mode issue; tcti,coop,dm,ctf,ff,scont,hold,unknown,a&d,aas,c&h,rpg,tdm,tvt,ans,ie&e,hunt,koth,obj,rc,vip
                    if (key == "t")
                        break;
                }
                _server = server;
            }

            static string JoinChar(IEnumerable<char> enumerable) => string.Join("", enumerable);

            bool ParseBool(string key) => _settings.ContainsKey(key) && _settings[key] == "t";

            string ParseString(string key) => _settings.ContainsKey(key) ? _settings[key] : null;

            int? ParseInt(string key) => _settings.ContainsKey(key) ? _settings[key].TryIntNullable() : null;

            double? ParseDouble(string key) => _settings.ContainsKey(key) ? _settings[key].TryDouble() : (double?) null;

            public void HandleTags() {
                _server.VerifySignatures = ParseBool("v") ? 2 : 0;
                _server.SvBattleye = ParseBool("b") ? 1 : 0;
                _server.ReqBuild = ParseInt("n");
                _server.Difficulty = ParseInt("i").GetValueOrDefault(0);
                _server.IsDedicated = ParseBool("d");
                _server.PasswordRequired = ParseBool("l");
                _server.GameState = ParseInt("s").GetValueOrDefault(0);
                _server.GameType = ParseString("t");
                _server.ServerPlatform = ParseString("p") == "w" ? ServerPlatform.Windows : ServerPlatform.Linux;
                _server.RequiredVersion = ParseInt("r");
                _server.Language = ParseInt("g");
                _server.Coordinates = ParseCoordinates(ParseString("c"));
            }

            static Coordinates ParseCoordinates(string coordinates) {
                if (coordinates == null)
                    return null;
                var split = coordinates.Split('-');
                return new Coordinates(split[0].TryDouble(), split[1].TryDouble());
            }
        }
    }
}
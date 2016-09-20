// <copyright company="SIX Networks GmbH" file="StarboundGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using SN.withSIX.Core.Services.Infrastructure;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Attributes;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Extensions;
using withSIX.Api.Models.Games;
using Player = SN.withSIX.Mini.Core.Games.Player;

namespace SN.withSIX.Mini.Plugin.Starbound.Models
{
    [Game(GameIds.Starbound,
        Executables = new[] {@"win64\starbound.exe", @"win32\starbound.exe"},
        ServerExecutables = new[] { @"win64\starbound_server.exe", @"win32\starbound_server.exe" },
        Name = "Starbound",
        IsPublic = true,
        Slug = "Starbound")]
    [SynqRemoteInfo(GameIds.Starbound)]
    [SteamInfo(SteamGameIds.Starbound, "Starbound")]
    [DataContract]
    public class StarboundGame : BasicSteamGame, IQueryServers
    {
        private readonly Lazy<IRelativeFilePath[]> _executables;
        private readonly Lazy<IRelativeFilePath[]> _serverExecutables;

        public StarboundGame(Guid id, StarboundGameSettings settings) : base(id, settings) {
            _executables =
                new Lazy<IRelativeFilePath[]>(() => Environment.Is64BitOperatingSystem
                    ? Metadata.GetExecutables().ToArray()
                    : Metadata.GetExecutables().Skip(1).ToArray());
            _serverExecutables =
                new Lazy<IRelativeFilePath[]>(() => Environment.Is64BitOperatingSystem
                    ? Metadata.GetServerExecutables().ToArray()
                    : Metadata.GetServerExecutables().Skip(1).ToArray());
        }

        protected override bool ShouldLaunchWithSteam(LaunchState ls) => false;

        protected override Task EnableMods(ILaunchContentAction<IContent> launchContentAction) {
            // TODO: PublisherId

            var content = launchContentAction.Content.SelectMany(x => x.Content.GetLaunchables(x.Constraint)).ToArray();
            var packages = content.OfType<IHavePackageName>()
                .Select(x => x.PackageName)
                .Distinct()
                .ToArray();
            HandleModDirectory(packages);

            return EnableModsInternal(content.OfType<IModContent>().Select(CreateMod), m => m.Enable());
        }

        protected override IEnumerable<IRelativeFilePath> GetExecutables(LaunchAction action) =>
            action == LaunchAction.LaunchAsDedicatedServer ? _serverExecutables.Value : _executables.Value;

        protected override IAbsoluteDirectoryPath GetDefaultDirectory()
            => GetGogDir("Starbound") ?? base.GetDefaultDirectory();

        private void HandleModDirectory(string[] packages) {
            var md = GetModInstallationDirectory();
            var di = md.DirectoryInfo;
            foreach (var f in GetPaksAndModPaks(di))
                HandleFileBasedMod(f, packages);
        }

        private static IEnumerable<IAbsoluteFilePath> GetPaksAndModPaks(DirectoryInfo di) => di.EnumerateFiles("*.pak")
            .Select(x => x.ToAbsoluteFilePath());

        private static void HandleFileBasedMod(IAbsoluteFilePath pak, IEnumerable<string> packages) {
            var pakBak = GetBackupFile(pak);
            if (packages.Contains(pak.FileNameWithoutExtension)) {
                if (!pak.Exists && pakBak.Exists)
                    pakBak.Move(pak);
            } else {
                if (!pak.Exists)
                    return;
                pakBak.DeleteIfExists();
                pak.Move(pakBak);
            }
        }

        //private static readonly string[] defaultStartupParameters = {"-noworkshop"};

        //protected override IEnumerable<string> GetStartupParameters() => defaultStartupParameters.Concat(base.GetStartupParameters());

        IAbsoluteDirectoryPath GetModInstallationDirectory()
            => InstalledState.Directory.GetChildDirectoryWithName("mods");

        protected override Task InstallMod(IModContent mod) {
            var m = CreateMod(mod);
            return m.Install(true);
        }

        protected override Task UninstallMod(IModContent mod) {
            var m = CreateMod(mod);
            return m.Uninstall();
        }

        private StarboundMod CreateMod(IModContent mod)
            =>
                new StarboundMod(mod, GetContentSourceDirectory(mod), GetModInstallationDirectory(),
                    InstalledState.Directory);

        public override Uri GetPublisherUrl(ContentPublisher c) {
            switch (c.Publisher) {
            case Publisher.Chucklefish:
                return new Uri(GetPublisherUrl(Publisher.Chucklefish), $"{c.PublisherId}");
            }
            throw new NotSupportedException($"The publisher is not currently supported {c.Publisher} for this game");
        }

        public override Uri GetPublisherUrl(Publisher c) {
            switch (c) {
            case Publisher.Chucklefish:
                return new Uri($"http://community.playstarbound.com/resources/");
            }
            throw new NotSupportedException($"The publisher is not currently supported {c} for this game");
        }

        public override Uri GetPublisherUrl() => GetPublisherUrl(Publisher.Chucklefish);

        class StarboundMod : SteamMod
        {
            private readonly IAbsoluteDirectoryPath _gameDir;
            private readonly IAbsoluteDirectoryPath _modDir;

            public StarboundMod(IModContent mod, IAbsoluteDirectoryPath sourceDir, IAbsoluteDirectoryPath modDir,
                IAbsoluteDirectoryPath gameDir) : base(sourceDir, mod) {
                _modDir = modDir;
                _gameDir = gameDir;
            }

            public Task Enable() => Install(false);

            protected override async Task InstallImpl(bool force) {
                _modDir.MakeSurePathExists();
                var exts = new[] { ".pak", ".modpak" };
                var destPakFile = _modDir.GetChildFileWithName($"{Mod.PackageName}.pak");
                if (!force && destPakFile.Exists) // TODO: Date check
                    return;
                
                // TODO: Support mods without Paks, as folder ? Or mark as not-installable
                var sourcePakFiles =
                    exts.SelectMany(x => SourcePath.DirectoryInfo.EnumerateFiles($"*{x}", SearchOption.AllDirectories))
                        .Select(x => x.ToAbsoluteFilePath()).ToArray();
                var sourcePak = sourcePakFiles.FirstOrDefault();
                IAbsoluteFilePath sourcePakPath;
                if (sourcePak == null || !sourcePak.Exists) {
                    var modInfo =
                        SourcePath.DirectoryInfo.EnumerateFiles("*.modinfo", SearchOption.AllDirectories)
                            .FirstOrDefault();
                    if (modInfo != null)
                        sourcePakPath = await PackModInfoMod(modInfo.ToAbsoluteFilePath()).ConfigureAwait(false);
                    else {
                        var metadata =
                            SourcePath.DirectoryInfo.EnumerateFiles(".metadata", SearchOption.AllDirectories)
                                .FirstOrDefault();
                        if (metadata == null) {
                            throw new NotInstallableException(
                                $"{Mod.PackageName} source .pak not found! You might try Diagnosing");
                        }
                        sourcePakPath = await PackMetadataMod(metadata.ToAbsoluteFilePath()).ConfigureAwait(false);
                    }
                } else
                    sourcePakPath = sourcePak;
                await sourcePakPath.CopyAsync(destPakFile).ConfigureAwait(false);
                var bak = GetBackupFile(destPakFile);
                bak.DeleteIfExists();
            }

            private async Task<IAbsoluteFilePath> PackModInfoMod(IAbsoluteFilePath modInfoPath) {
                await modInfoPath.CopyAsync(modInfoPath.GetBrotherFileWithName("pak.modinfo")).ConfigureAwait(false);
                return await CreatePakFile(modInfoPath.ParentDirectoryPath).ConfigureAwait(false);
            }

            private Task<IAbsoluteFilePath> PackMetadataMod(IAbsoluteFilePath metadataPath)
                => CreatePakFile(metadataPath.ParentDirectoryPath);

            private async Task<IAbsoluteFilePath> CreatePakFile(IAbsoluteDirectoryPath sourceDir) {
                var sourcePakPath =
                    Path.GetTempPath().ToAbsoluteDirectoryPath().GetChildFileWithName($"{Mod.PackageName}.pak");
                // TODO: Delete after usage
                var toolDir = _gameDir.GetChildDirectoryWithName("win32");
                await CreatePakFile(sourceDir, toolDir, sourcePakPath).ConfigureAwait(false);
                return sourcePakPath;
            }

            private static async Task CreatePakFile(IAbsoluteDirectoryPath sourceDir, IAbsoluteDirectoryPath toolDir,
                IAbsoluteFilePath sourcePakPath) {
                var r = await
                    Tools.ProcessManager.LaunchAndGrabAsync(
                        new BasicLaunchInfo(new ProcessStartInfo(
                            toolDir.GetChildFileWithName(@"asset_packer.exe").ToString(),
                            new[] {sourceDir.ToString(), sourcePakPath.ToString()}
                                .CombineParameters()))).ConfigureAwait(false);
                if (r.ExitCode != 0) {
                    throw new Exception(
                        $"Failed creating a pak file for the mod. Code: {r.ExitCode} Output: {r.StandardOutput} Error: {r.StandardError}");
                }
            }

            protected override async Task UninstallImpl() {
                if (!_modDir.Exists)
                    return;
                new[] {
                    _modDir.GetChildFileWithName($"{Mod.PackageName}.pak"),
                    _modDir.GetChildFileWithName($"{Mod.PackageName}.modpak")
                }.Where(x => x.Exists).ForEach(x => x.Delete());
            }
        }

        public async Task<List<IPEndPoint>> GetServers() {
            var master = new SourceMasterQuery("starbound");
            var r = await master.GetParsedServers().ConfigureAwait(false);
            return r.Select(x => x.Address).ToList();
        }

        private static readonly SourceQueryParser sourceQueryParser = new SourceQueryParser();

        public async Task<List<ServerInfo>> GetServerInfos(IReadOnlyCollection<IPEndPoint> addresses,
            bool inclPlayers = false) {
            var infos = new List<ServerInfo>();
            // TODO: Use serverquery queue ?
            foreach (var a in addresses) {
                var serverInfo = new ServerInfo { Address = a };
                infos.Add(serverInfo);
                var server = new Server(serverInfo);
                using (
                    var serverQueryState = new ServerQueryState(server, sourceQueryParser) { HandlePlayers = inclPlayers }
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

        // TODO: Customize
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
                Info.Status = (int)status;
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
                        .Select(x => new Player { Name = x.Name, Score = x.Score, Duration = x.Duration })
                        .ToList();
            }

            public IPEndPoint Address { get; }

            static Version GetVersion(string version) => version?.TryParseVersion();

            static IEnumerable<string> GetList(IEnumerable<KeyValuePair<string, string>> dict, string keyWord) {
                var rx = GetRx(keyWord);
                return string.Join("", (from kvp in dict.Where(x => x.Key.StartsWith(keyWord))
                                        let w = rx.Match(kvp.Key)
                                        where w.Success
                                        select new { Index = w.Groups[1].Value.TryInt(), Total = w.Groups[2].Value.TryInt(), kvp.Value })
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

                double? ParseDouble(string key) => _settings.ContainsKey(key) ? _settings[key].TryDouble() : (double?)null;

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
}
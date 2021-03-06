﻿// <copyright company="SIX Networks GmbH" file="RealVirtualityGame.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Api.Models;
using withSIX.Api.Models.Extensions;
using withSIX.Core;
using withSIX.Core.Extensions;
using withSIX.Mini.Core.Extensions;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Services.ContentInstaller;
using withSIX.Mini.Core.Games.Services.GameLauncher;
using withSIX.Mini.Plugin.Arma.Attributes;
using withSIX.Mini.Plugin.Arma.Services;

namespace withSIX.Mini.Plugin.Arma.Models
{
    [DataContract]
    public abstract class RealVirtualityGame : BasicGame, ILaunchWith<IRealVirtualityLauncher>
    {
        public const string BohemiaStudioRegistry = @"SOFTWARE\Bohemia Interactive Studio";
        public const string BohemiaRegistry = @"SOFTWARE\Bohemia Interactive";

        readonly Lazy<IAbsoluteDirectoryPath> _keysPath;
        private RealVirtualityGameSettings _settings => (RealVirtualityGameSettings)Settings;

        [Obsolete("deprecate")] private readonly Lazy<bool> _shouldLaunchAsDedicatedServer;
        readonly Lazy<IAbsoluteDirectoryPath> _userconfigPath;

        protected RealVirtualityGame(Guid id, RealVirtualityGameSettings settings) : base(id, settings) {
            ProfileInfo = this.GetMetaData<RvProfileInfoAttribute>();
            _shouldLaunchAsDedicatedServer = new Lazy<bool>(() => {
                var casted = Settings as ILaunchAsDedicatedServer;
                return (casted?.LaunchAsDedicatedServer).GetValueOrDefault();
            });
            _keysPath =
                new Lazy<IAbsoluteDirectoryPath>(() => InstalledState.Directory.GetChildDirectoryWithName("keys"));
            _userconfigPath =
                new Lazy<IAbsoluteDirectoryPath>(() => InstalledState.Directory.GetChildDirectoryWithName("userconfig"));
        }

        RvProfileInfoAttribute ProfileInfo { get; }
        private IAbsoluteDirectoryPath KeysPath => _keysPath.Value;

        private IAbsoluteDirectoryPath UserconfigPath => _userconfigPath.Value;

        protected override void SetupDefaultDirectories(GameSettings settings) {
            var s = (RealVirtualityGameSettings) settings;
            if (s.GameDirectory == null)
                s.GameDirectory = GetDefaultDirectory();
            if (s.PackageDirectory == null)
                s.PackageDirectory = GetDefaultPackageDirectory();
            if ((s.RepoDirectory == null) && (s.PackageDirectory != null))
                s.RepoDirectory = s.PackageDirectory;
        }

        protected override Task ScanForLocalContentImpl()
            => TaskExt.StartLongRunningTask(() => ScanForLocalContentInternal());

        void ScanForLocalContentInternal() {
            var existingModFolders = GetExistingModFolders().ToArray();
            var newContent = new RvContentScanner(this).ScanForNewContent(Dlcs.Select(x => x.PackageName).ToList(), existingModFolders).ToArray();
            var removedContent = InstalledContent.OfType<IPackagedContent>()
                .Where(x => !ModExists(x, existingModFolders))
                .Cast<Content>();
            ProcessAddedAndRemovedContent(newContent, removedContent);
        }

        static bool ModExists(IHavePackageName content, IEnumerable<IAbsoluteDirectoryPath> directories)
            => directories.Any(x => ContentExists(x.GetChildDirectoryWithName(content.PackageName)));

        IEnumerable<IAbsoluteDirectoryPath> GetExistingModFolders() => GetModFolders().Where(x => x.Exists);

        IEnumerable<IAbsoluteDirectoryPath> GetModFolders() {
            if (ContentPaths.IsValid)
                yield return ContentPaths.Path;
            if (InstalledState.IsInstalled)
                yield return InstalledState.Directory;
        }

        IAbsoluteDirectoryPath GetDefaultPackageDirectory()
            => Common.Paths.MyDocumentsPath.GetChildDirectoryWithName(ProfileInfo.DocumentsMainName);

        protected override InstallContentAction GetInstallAction(
                IDownloadContentAction<IInstallableContent> action)
            => new InstallContentAction(action.Content, action.CancelToken) {
                RemoteInfo = RemoteInfo,
                Paths = ContentPaths,
                Game = this,
                Cleaning = ContentCleaning,
                Force = action.Force,
                HideLaunchAction = action.HideLaunchAction,
                Name = action.Name
            };

        protected override IAbsoluteDirectoryPath GetContentDirectory() => _settings.PackageDirectory;

        protected override async Task<Process> LaunchImpl(IGameLauncherFactory factory,
            ILaunchContentAction<IContent> action) {
            var launcher = factory.Create<IRealVirtualityLauncher>(this);
            var launchAction = _shouldLaunchAsDedicatedServer.Value
                ? LaunchAction.LaunchAsDedicatedServer
                : action.Action;
            var ls = new LaunchState(GetLaunchExecutable(launchAction), GetExecutable(launchAction),
                await GetStartupParameters(launcher, action, launchAction).ConfigureAwait(false), launchAction);
            if (launchAction.IsAsServer())
                Tools.FileUtil.Ops.CreateDirectoryAndSetACLWithFallbackAndRetry(KeysPath);

            var launchables = action.GetLaunchables()
                .OfType<IModContent>()
                .Select(c => new RvMod(this, c));

            foreach (var mod in launchables)
                await mod.PreLaunch(action.Action).ConfigureAwait(false);
            return await InitiateLaunch(launcher, ls).ConfigureAwait(false);
        }

        protected override bool ShouldLaunchWithSteam(LaunchState ls) {
            var casted = Settings as ILaunchAsDedicatedServer;
            var launchAsDedicated = (casted?.LaunchAsDedicatedServer).GetValueOrDefault();
            return !launchAsDedicated && base.ShouldLaunchWithSteam(ls);
        }

        Tuple<string[], string[]> RvStartupParameters(IRealVirtualityLauncher launcher,
                ILaunchContentAction<IContent> action, LaunchAction launchAction)
            => GetStartupBuilder().GetStartupParameters(GetStartupSpec(action, launchAction));

        protected virtual StartupBuilder GetStartupBuilder() => new StartupBuilder(this, new ModListBuilder());

        protected virtual async Task<IReadOnlyCollection<string>> GetStartupParameters(IRealVirtualityLauncher launcher,
            ILaunchContentAction<IContent> action, LaunchAction launchAction) {
            var startupInfo = RvStartupParameters(launcher, action, launchAction);
            var startupParameters = startupInfo.Item1.ToList();
            var parData = startupInfo.Item2;
            if (!parData.Any())
                return startupParameters;

            try {
                var parPath =
                    await
                        launcher.WriteParFile(new WriteParFileInfo(Id, string.Join("\n", parData)
                                //, new ShortGuid(Settings.CurrentProfile).ToString()
                            ))
                            .ConfigureAwait(false);
                startupParameters.Insert(0, "-par=" + parPath);
            } catch (Exception e) {
                throw new ParFileException(e.Message, e);
            }

            return startupParameters;
        }

        protected override Task InstallImpl(IContentInstallationService installationService,
            IDownloadContentAction<IInstallableContent> action) {
            foreach (var m in GetPackagedContent(action.Content).Select(x => new RvMod(this, x)))
                m.Register();
            return base.InstallImpl(installationService, action);
        }

        StartupBuilderSpec GetStartupSpec(ILaunchContentAction<IContent> action, LaunchAction launchAction) {
            var content = GetLaunchables(action);

            return new StartupBuilderSpec {
                GamePath = InstalledState.Directory,
                SteamWorkshopPath = SteamDirectories.IsValid ? SteamDirectories.Workshop.ContentPath : null,
                LaunchType = launchAction == LaunchAction.LaunchAsServer ? LaunchType.Multiplayer : action.LaunchType,
                ModPath = ContentPaths.Path,
                GameVersion = InstalledState.Version,
                InputMods = content.OfType<IModContent>().Select(x => new RvMod(this, x)).ToList(), //.Where(x => x.Controller.Exists),
                AdditionalLaunchMods = GetAdditionalLaunchMods(),
                //Mission = CalculatedSettings.Mission,
                Server = GetServerOverride(action) ?? GetServerFromContent(content, launchAction),
                // TODO: Or configurable by user?
                StartupParameters = Settings.StartupParameters.Get(),
                UseParFile = true
            };
        }

        CollectionServer GetServerFromContent(IEnumerable<ILaunchableContent> content, LaunchAction launchAction) {
            if (launchAction == LaunchAction.Default)
                return content.OfType<IHaveServers>().FirstOrDefault()?.Servers.FirstOrDefault();
            return launchAction == LaunchAction.Join
                ? content.OfType<IHaveServers>().First().Servers.First()
                : null;
        }

        protected virtual IEnumerable<IAbsoluteDirectoryPath> GetAdditionalLaunchMods()
            => Enumerable.Empty<IAbsoluteDirectoryPath>();

        public override IAbsoluteDirectoryPath GetConfigPath() {
            ConfirmInstalled();
            return UserconfigPath;
        }

        public override IAbsoluteDirectoryPath GetConfigPath(IPackagedContent content) {
            ConfirmInstalled();
            return UserconfigPath.GetChildDirectoryWithName(content.PackageName.StartsWith("@")
                ? content.PackageName.Substring(1)
                : content.PackageName);
        }

        protected internal class RvMod
        {
            private static readonly UserconfigProcessor ucp = new UserconfigProcessor();
            private readonly IContentWithPackageName _content;
            private readonly RealVirtualityGame _game;
            private readonly IAbsoluteDirectoryPath _myPath;

            public RvMod(RealVirtualityGame game, IContentWithPackageName content) {
                _game = game;
                _content = content;
                _myPath = game.ContentPaths.Path.GetChildDirectoryWithName(content.PackageName);
            }

            public string PackageName => _content.PackageName;

            public IAbsoluteDirectoryPath GetSourcePath() => _content.GetSourceDirectory(_game);

            internal void Register() => _content.RegisterAdditionalPostInstallTask(PostInstall);

            private async Task PostInstall(bool processed) {
                await InstallUserconfig(processed).ConfigureAwait(false);
            }

            public async Task PreLaunch(LaunchAction action) {
                if (action.IsAsServer())
                    await InstallBikeys().ConfigureAwait(false);
            }

            private Task<string> InstallUserconfig(bool processed)
                => ucp.ProcessUserconfig(_myPath, _game.InstalledState.Directory, null, processed);

            private async Task InstallBikeys() {
                var keysPath = _game.KeysPath;
                foreach (var key in GetBiKeys())
                    await key.CopyAsync(keysPath.GetChildFileWithName(key.FileName), true, true).ConfigureAwait(false);
            }

            IEnumerable<IAbsoluteFilePath> GetBiKeys()
                => new[] {_myPath.GetChildDirectoryWithName("keys"), _myPath.GetChildDirectoryWithName("store\\keys")}
                    .Where(x => x.Exists).SelectMany(GetBiKeysFromPath);

            static IEnumerable<IAbsoluteFilePath> GetBiKeysFromPath(IAbsoluteDirectoryPath path)
                => path.ChildrenFilesPath.Where(x => x.HasExtension(".bikey"));
        }

        // TODO: Local mods in separate folders (Custom path) support
        protected internal class ModListBuilder
        {
            static readonly ArmaModPathValidator modPathValidator = new ArmaModPathValidator();
            protected List<RvMod> InputMods { get; set; }
            protected List<IAbsoluteDirectoryPath> OutputMods { get; set; }
            protected ModListBuilderSpec Spec { get; set; }

            public IEnumerable<string> ProcessModList(ModListBuilderSpec spec) {
                Spec = spec;
                OutputMods = new List<IAbsoluteDirectoryPath>();
                InputMods = Spec.InputMods.ToList();

                ProcessMods();

                return CleanModList(OutputMods).DistinctBy(x => x.ToLower());
            }

            protected IEnumerable<IAbsoluteDirectoryPath> GetModPaths(RvMod arg)
                => Enumerable.Repeat(CombineModPathIfNeeded(arg), 1);

            IAbsoluteDirectoryPath CombineModPathIfNeeded(RvMod mod) => mod.GetSourcePath();

            static string SubMod(IModContent mod, string name) => mod.PackageName + "/" + name;

            protected virtual void ProcessMods() {
                AddPrimaryGameFolders();
                AddNormalMods();
            }

            protected void AddPrimaryGameFolders() {
                OutputMods.AddRange(Spec.AdditionalLaunchMods);
            }

            void AddNormalMods() {
                OutputMods.AddRange(InputMods.SelectMany(GetModPaths));
            }

            IEnumerable<string> CleanModList(IEnumerable<IAbsoluteDirectoryPath> modList) => modList.Distinct()
                .Where(x => modPathValidator.Validate(x))
                .Select(CleanModPath).DistinctBy(x => x.ToLower());

            string CleanModPath(IAbsoluteDirectoryPath fullModPath) => fullModPath.GetRelativeDirectory(Spec.GamePath);
        }

        protected internal class ModListBuilderSpec
        {
            public IEnumerable<IAbsoluteDirectoryPath> AdditionalLaunchMods { get; set; } =
                new IAbsoluteDirectoryPath[0];
            public IAbsoluteDirectoryPath GamePath { get; set; }
            public IAbsoluteDirectoryPath SteamWorkshopPath { get; set; }
            public IReadOnlyCollection<RvMod> InputMods { get; set; } = new RvMod[0];
            public IAbsoluteDirectoryPath ModPath { get; set; }
        }

        protected class RvProfileInfo
        {
            public RvProfileInfo(string mainName, string otherProfilesName, string profileExtension) {
                if (!(!string.IsNullOrWhiteSpace(mainName))) throw new ArgumentNullException("!string.IsNullOrWhiteSpace(mainName)");
                if (!(!string.IsNullOrWhiteSpace(otherProfilesName))) throw new ArgumentNullException("!string.IsNullOrWhiteSpace(otherProfilesName)");
                if (!(!string.IsNullOrWhiteSpace(profileExtension))) throw new ArgumentNullException("!string.IsNullOrWhiteSpace(profileExtension)");

                DocumentsMainName = mainName;
                DocumentsOtherProfilesName = otherProfilesName;
                ProfileExtension = profileExtension;
            }

            public string DocumentsMainName { get; }
            public string DocumentsOtherProfilesName { get; }
            public string ProfileExtension { get; }
        }

        protected internal class StartupBuilder
        {
            readonly string[] _doesNotSupportPar = {
                "-malloc=", "-cpuCount=", "-exThreads=", "-maxMem=", "-maxVram=",
                "-enableHT", "-profiles=", "-cfg="
            };
            readonly ModListBuilder _modListBuilder;
            readonly bool _supportsMissions;
            readonly bool _supportsMods;
            readonly bool _supportsServers;
            List<string> _arguments;
            List<string> _par;
            StartupBuilderSpec _spec;

            public StartupBuilder(RealVirtualityGame game) {
                if (game == null) throw new ArgumentNullException(nameof(game));
                //_supportsMods = game.SupportsMods();
                //_supportsMissions = game.SupportsMissions();
                //_supportsServers = game.SupportsServers();

                if (_supportsMods)
                    _modListBuilder = new ModListBuilder();
            }

            public StartupBuilder(RealVirtualityGame game, ModListBuilder builder) {
                if (game == null) throw new ArgumentNullException(nameof(game));
                if (builder == null) throw new ArgumentNullException(nameof(builder));
                _supportsMods = true;
                //_supportsMissions = game.SupportsMissions();
                //_supportsServers = game.SupportsServers();
                _modListBuilder = builder;
            }

            internal Tuple<string[], string[]> GetStartupParameters(StartupBuilderSpec spec) {
                _spec = spec;
                _arguments = new List<string>();
                _par = new List<string>();

                DealWithLaunchType();

                //if (_supportsServers)
                AddServerArgs();
                if (_supportsMods)
                    AddModArgs();
                //if (_supportsMissions)
                //AddMissionArgs();

                //AddCommandArgs();
                AddGameArgs();

                return Tuple.Create(_arguments.Distinct().ToArray(), _par.Distinct().ToArray());
            }

            void DealWithLaunchType() {
                switch (_spec.LaunchType) {
                case LaunchType.Multiplayer: {
                    AddArgOrPar("-host");
                    break;
                }
                /*
                case LaunchType.Editor: {
                    AddArgOrPar("-editor");
                    break;
                }*/
                }
            }

            void AddArgOrPar(string arg) {
                if (_spec.UseParFile)
                    _par.Add(arg);
                else
                    _arguments.Add(arg);
            }

            /*
        void AddMissionArgs()
        {
            var missionFolder = _spec.Mission as MissionFolder;
            if (missionFolder == null)
                AddMissionFileArgs();
            else
                AddMissionFolderArgs(missionFolder);
        }

    void AddMissionFolderArgs(MissionFolder missionFolder)
    {
        if (missionFolder == null)
            return;

        var folder = missionFolder.CustomPath.GetChildDirectoryWithName(missionFolder.FolderName);
        if (_spec.UseParFile)
            _par.Add(GetMissionFolderParameter(folder));
        else
            _arguments.Add(GetMissionFolderParameter(folder));
    }

        static string GetMissionFolderParameter(IAbsoluteDirectoryPath missionFolder)
            {
                return missionFolder + "\\mission.sqm";
            }

            void AddMissionFileArgs()
            {
                if (_spec.Mission == null)
                    return;

                var param = GetMissionParameter(_spec.Mission);
                if (param == null)
                    return;

                if (_spec.UseParFile)
                    _par.Add(param);
                else
                    _arguments.Add(param);
            }

            static string GetMissionParameter(MissionBase mission)
            {
                var missionFile = mission.Controller.GetMissionFile();
                if (missionFile == null)
                    return null;

                var folder = missionFile as IAbsoluteDirectoryPath;
                return folder != null
                    ? GetMissionFolderParameter(folder)
                    : GetMissionParameterBasedOnType(mission, (IAbsoluteFilePath)missionFile);
            }

            static string GetMissionParameterBasedOnType(MissionBase mission, IAbsoluteFilePath missionFile)
            {
                return mission.Type == MissionTypes.MpMission
                    ? "-host"
                    : string.Format("-init=playMission['', '{0}']", missionFile.FileNameWithoutExtension);
            }
                        */

            void AddGameArgs() {
                if (_spec.UseParFile)
                    AddGameArgsToParFileAndArguments();
                else
                    AddGameArgsToArguments();
            }

            void AddGameArgsToParFileAndArguments() {
                _arguments.AddRange(_spec.StartupParameters.Where(ShouldBeOnStartupLine));
                _par.AddRange(_spec.StartupParameters.Where(ShouldBeOnParLine));
            }

            void AddGameArgsToArguments() {
                _arguments.AddRange(_spec.StartupParameters);
            }

            bool ShouldBeOnStartupLine(string arg)
                => _doesNotSupportPar.Any(x => arg.StartsWith(x, StringComparison.OrdinalIgnoreCase));

            bool ShouldBeOnParLine(string arg) => !ShouldBeOnStartupLine(arg);

            /*
            void AddCommandArgs() {
                var gv = _spec.GameVersion;
                if (gv == null || gv.Revision < 100399)
                    return;
                var arg = $"-command={CommandAPI.DefaultPipeTag}";
                if (_spec.UseParFile)
                    _par.Add(arg);
                else
                    _arguments.Add(arg);
            }
            */

            void AddModArgs() {
                var modStr = string.Join(";", _modListBuilder.ProcessModList(new ModListBuilderSpec {
                    InputMods = _spec.InputMods,
                    GamePath = _spec.GamePath,
                    SteamWorkshopPath = _spec.SteamWorkshopPath,
                    ModPath = _spec.ModPath,
                    AdditionalLaunchMods = _spec.AdditionalLaunchMods
                }));

                var arg = $"-mod={modStr}";
                if (_spec.UseParFile)
                    _par.Add(arg);
                else
                    _arguments.Add(arg);
            }

            void AddServerArgs() {
                //if (_spec.LaunchAction == LaunchAction.LaunchAsServer) {
                //  AddArgOrPar("-server");
                //}

                if (_spec.Server == null)
                    return;
                var args = GetServerArgs().ToArray();
                if (_spec.UseParFile) {
                    _par.AddRange(args);
                    _arguments.AddRange(args.Take(2));
                    // To pick up running games with server, but leave password off because it breaks password usage in the par file :S
                } else
                    _arguments.AddRange(args);
            }

            /*
            static IEnumerable<IPAddress> GetSystemIpAddresses()
                => from netInterface in NetworkInterface.GetAllNetworkInterfaces()
                    select netInterface.GetIPProperties()
                    into ipProps
                    from addr in ipProps.UnicastAddresses
                    select addr.Address;

            IPAddress[] TryGetSystemIpAddresses() {
                try {
                    return GetSystemIpAddresses().ToArray();
                } catch (Exception ex) {
                    MainLog.Logger.FormattedWarnException(ex, "Failure to determine ip addresses");
                    return new IPAddress[0];
                }
            }
            */

            IEnumerable<string> GetServerArgs() {
                var sa = _spec.Server.Address;
                var args = new List<string> {
                    "-connect=" + GetServerIP(sa.Address),
                    "-port=" + sa.Port
                };

                if (!string.IsNullOrWhiteSpace(_spec.Server.Password))
                    args.Add("-password=" + _spec.Server.Password);
                return args;
            }

            IPAddress GetServerIP(IPAddress ip) => ip;
            //TryGetSystemIpAddresses().Contains(ip) ? GetLoopbackIP(ip) : ip;

            static IPAddress GetLoopbackIP(IPAddress ip)
                => ip.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;
        }

        public interface IModPathValidator
        {
            bool Validate(string path);
        }

        public abstract class PathValidator
        {
            static bool IsValidPath(string path) => Tools.FileUtil.IsValidRootedPath(path);

            protected static bool HasSubFolder(string path, string subFolder)
                => Directory.Exists(Path.Combine(path, subFolder));

            protected static bool ValidateBasics(string path) => IsValidPath(path) && Directory.Exists(path);
        }

        public class ArmaModPathValidator : PathValidator, IModPathValidator
        {
            static readonly string[] gameDataDirectories = {"addons", "dta", "common", "dll"};

            public bool Validate(string path) => ValidateBasics(path) && ValidateSpecials(path);

            public bool Validate(IAbsoluteDirectoryPath path) => Validate(path.ToString());

            static bool ValidateSpecials(string path) => gameDataDirectories.Any(dir => HasSubFolder(path, dir));
        }

        internal class StartupBuilderSpec
        {
            public IEnumerable<IAbsoluteDirectoryPath> AdditionalLaunchMods { get; set; } =
                new IAbsoluteDirectoryPath[0];
            public IAbsoluteDirectoryPath GamePath { get; set; }
            public Version GameVersion { get; set; }
            public IReadOnlyCollection<RvMod> InputMods { get; set; } = new RvMod[0];
            //public MissionBase Mission { get; set; }
            public IAbsoluteDirectoryPath ModPath { get; set; }
            public CollectionServer Server { get; set; }
            public IEnumerable<string> StartupParameters { get; set; } = new string[0];
            public bool UseParFile { get; set; }
            public LaunchType LaunchType { get; set; }
            public IAbsoluteDirectoryPath SteamWorkshopPath { get; set; }
        }
    }

    public class ParFileException : Exception
    {
        public ParFileException(string message, Exception exception) : base(message, exception) {}
    }
}
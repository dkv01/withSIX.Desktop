// <copyright company="SIX Networks GmbH" file="ModController.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Play.Core.Games.Entities;
using withSIX.Play.Core.Games.Legacy.Arma;

namespace withSIX.Play.Core.Games.Legacy.Mods
{
    public class ModController : ContentController, IEnableLogging
    {
        static readonly EnumerateSignatures enumerateSignatures = new EnumerateSignatures();
        static readonly UserconfigProcessor userconfigProcessor = new UserconfigProcessor();
        readonly IContentEngine _contentEngine;
        readonly ModState _modState;
        readonly SixSyncModInstaller _sixSyncModInstaller;
        IModS _script;

        public ModController()
            : base(null) {}

        public ModController(IMod mod)
            : base(mod) {
            Contract.Requires<ArgumentNullException>(mod != null);
            _contentEngine = CalculatedGameSettings.ContentManager.ContentEngine;
            Mod = mod;
            TryGetModScript();
            _modState = new ModState(mod);
            _sixSyncModInstaller = new SixSyncModInstaller(mod, _modState);

            Model.State = _modState.State;
        }

        public IMod Mod { get; }
        public virtual IAbsoluteDirectoryPath Path => _modState.Path;
        public bool Exists => _modState.Exists;
        public virtual ISupportModding Game { get; set; }

        void TryGetModScript() {
            if (_contentEngine.ModHasScript(Mod.NetworkId))
                _script = _contentEngine.LoadModS(Mod, true);
        }

        public void TryProcessModAppsAndUserconfig(Game currentGame, bool forceUserconfig = false) {
            if (DomainEvilGlobal.Settings.ModOptions.AutoProcessModApps)
                TryProcessModApps(currentGame);
            TryProcessUserconfig(currentGame, forceUserconfig);
        }

        public async Task ConvertOrInstallOrUpdateSixSync(bool force, StatusRepo statusRepo,
            IAbsoluteDirectoryPath packPath) {
            UpdateState();
            try {
                await
                    _sixSyncModInstaller.ConvertOrInstallOrUpdateInternal(Path, force, statusRepo, _modState, packPath)
                        .ConfigureAwait(false);
            } finally {
                UpdateState();
            }
        }

        public bool HasMultipleVersions() => Package != null && Package.ActualPackages.Count > 1;

        public override sealed UpdateState CreateUpdateState() => new UpdateState(Mod) {
            CurrentRevision = Revision,
            Revision = DesiredRevision,
            Size = Mod.Size,
            SizeWd = Mod.SizeWd
        };

        public void UpdateState(ISupportModding game) {
            Game = game;
            UpdateState();
        }

        public void UpdateState() {
            UpdateModState();
            Model.State = _modState.State;
            Revision = _modState.Revision;
            DesiredRevision = _modState.DesiredRevision;
            LatestRevision = _modState.LatestRevision;
            NewerVersionAvailable = Revision != LatestRevision;
        }

        public void Uninstall() {
            Tools.FileUtil.Ops.DeleteWithRetry(Path.ToString());
            Package?.Remove();
        }

        public IEnumerable<string> GetSignatures() => !Path.Exists ? Enumerable.Empty<string>() : enumerateSignatures.Enumerate(Path);

        public void ProcessBetaFiles(IProcessManager processManager) {
            var beFile = Path.GetChildFileWithName("setup_battleyearma2oa.exe");
            if (beFile.Exists)
                processManager.Launch(new BasicLaunchInfo(new ProcessStartInfo(beFile.ToString())));
        }

        public IEnumerable<IAbsoluteFilePath> GetBiKeys() => new[] { Path.GetChildDirectoryWithName("keys"), Path.GetChildDirectoryWithName("store\\keys") }
    .Where(x => x.Exists).SelectMany(GetBiKeysFromPath);

        static IEnumerable<IAbsoluteFilePath> GetBiKeysFromPath(IAbsoluteDirectoryPath path) => path.ChildrenFilesPath.Where(x => x.HasExtension(".bikey"));

        void UpdateModState() {
            var package = Package;
            if (Mod is LocalMod)
                _modState.UpdateLocalModState(Game);
            else if (package != null)
                _modState.UpdateSynqState(Game, package);
            else
                _modState.UpdateSixSyncState(Game);
        }

        public override string ToString() => GetType().Name + ": " + Mod;

        public static string ConvertState(ContentState state) {
            switch (state) {
            case ContentState.NotInstalled: {
                return "Install";
            }
            case ContentState.UpdateAvailable: {
                return "Update";
            }
            case ContentState.Unverified: {
                return "Diagnose";
            }
            case ContentState.Uptodate: {
                return "Diagnose";
            }
            }

            return null;
        }

        void ProcessUserconfig(IHaveInstalledState currentGame, bool force = true) {
            var checksum = userconfigProcessor.ProcessUserconfig(Path, currentGame.InstalledState.Directory,
                Mod.UserConfigChecksum, force);
            if (checksum != null)
                Mod.UserConfigChecksum = checksum;
        }

        void TryProcessUserconfig(IHaveInstalledState currentGame, bool forceUserconfig) {
            try {
                ProcessUserconfig(currentGame, forceUserconfig);
            } catch (IOException e) {
                // It is extremly hard to figure out failure with the DirectoryCopy method :S
                // The Data property has info, but it is probably localized in the OS language.
                // If Overwrite is true, THhen the exception could be warranted.
                this.Logger().FormattedWarnException(e);
            } catch (Exception e) {
                Tools.InformUserError(null,
                    "Failure during processing of mod userconfig for " + Mod.Name, e);
            }
        }

        void TryProcessModApps(Game currentGame) {
            try {
                //if (_script == null)
                TryGetModScript();
                _script?.processMod();
            } catch (Exception e) {
                Tools.InformUserError(null,
                    "Failure during processing of mod apps for " + Mod.Name, e);
            }
        }
    }
}
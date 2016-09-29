// <copyright company="SIX Networks GmbH" file="GamesPreLaunchEventHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Play.Applications.Services;
using SN.withSIX.Play.Core;
using SN.withSIX.Play.Core.Connect;
using SN.withSIX.Play.Core.Games.Entities;
using SN.withSIX.Play.Core.Games.Legacy;
using SN.withSIX.Play.Core.Games.Legacy.Arma;
using SN.withSIX.Play.Core.Games.Legacy.Events;
using SN.withSIX.Play.Core.Games.Legacy.Mods;
using SN.withSIX.Play.Core.Options;

namespace SN.withSIX.Play.Applications.NotificationHandlers
{
    public class GamesPreLaunchEventHandler : IApplicationService
    {
        readonly IDialogManager _dialogManager;
        readonly IronFrontService _ifService;
        readonly IRestarter _restarter;
        private readonly ISpecialDialogManager _specialDialogManager;
        readonly UserSettings _settings;
        PreGameLaunchCancelleableEvent _event;

        public GamesPreLaunchEventHandler(IDialogManager dialogManager, UserSettings settings,
            IronFrontService ifService, IRestarter restarter, ISpecialDialogManager specialDialogManager) {
            _dialogManager = dialogManager;
            _settings = settings;
            _ifService = ifService;
            _restarter = restarter;
            _specialDialogManager = specialDialogManager;
        }

        public async Task Process(PreGameLaunchCancelleableEvent message) {
            _event = message;

            if (!message.Cancel)
                message.Cancel = await ShouldCancel().ConfigureAwait(false);

            // TODO: The data to check should already be included, like Executable/Directory, etc?
            // Or even better; include a flag that says if there are already running instances of the game instead?
            if (!message.Cancel)
                await CheckRunningProcesses(GetGameExeName()).ConfigureAwait(false);
        }

        async Task<bool> ShouldCancel()
            => !await ConfirmNoUpdatesAvailable() || !await HandleIronFront().ConfigureAwait(false)
               || (_event.Server != null && !await CheckLaunchServer());

        async Task<bool> HandleIronFront() => !(_ifService.IsIronFrontEnabled(_event.Collection) && !await InstallIronFront().ConfigureAwait(false));

        async Task<bool> InstallIronFront() {
            try {
                if (_ifService.IsIronFrontInstalled(DomainEvilGlobal.SelectedGame.ActiveGame))
                    return true;

                if (await _dialogManager.MessageBox(new MessageBoxDialogParams(
                    @"Please confirm the installation of Iron Front in Arma by clicking OK.

Note: The conversion and patching process will take several minutes - please be patient.",
                    "Iron Front in Arma setup", SixMessageBoxButton.OKCancel)) == SixMessageBoxResult.Cancel)
                    return false;
                _ifService.InstallIronFrontArma(DomainEvilGlobal.SelectedGame.ActiveGame);
            } catch (OaIronfrontNotFoundException) {
                await _dialogManager.MessageBox(new MessageBoxDialogParams("OA/IronFront not found?"));
                return false;
            } catch (DestinationDriveFullException ddex) {
                await _dialogManager.MessageBox(new MessageBoxDialogParams(
                    "You have not enough space on the destination drive, you need at least " +
                    Tools.FileUtil.GetFileSize(ddex.RequiredSpace) + " free space on " +
                    ddex.Path));
                return false;
            } catch (TemporaryDriveFullException tdex) {
                await _dialogManager.MessageBox(new MessageBoxDialogParams(
                    "You have not enough space on the temp drive, you need at least " +
                    Tools.FileUtil.GetFileSize(tdex.RequiredSpace) + " free space on " +
                    tdex.Path));
                return false;
            } catch (ElevationRequiredException erex) {
                if (await _dialogManager.MessageBox(new MessageBoxDialogParams(
                    "The IronFront conversion process needs to run as administrator, restarting now, please start the process again after restarted")) ==
                    SixMessageBoxResult.Cancel)
                    return false;
                _restarter.RestartWithUacInclEnvironmentCommandLine();
                return false;
            } catch (UnsupportedIFAVersionException e) {
                await _dialogManager.MessageBox(new MessageBoxDialogParams(
                    "Unsupported Iron Front version. Must either be 1.0, 1.03, 1.04, or 1.05\nFound: " +
                    e.Message));
                return false;
            }
            return true;
        }

        async Task<bool> ConfirmNoUpdatesAvailable() {
            if (_event.Game.CalculatedSettings.State == OverallUpdateState.Play)
                return true;

            var custom = _event.Collection as CustomCollection;
            if (custom == null || !custom.ForceModUpdate) {
                return
                    (await _dialogManager.MessageBox(
                        new MessageBoxDialogParams(
                            "There appear to be updates available to install, are you sure you want to launch without installing them?",
                            "Are you sure?", SixMessageBoxButton.YesNo))).IsYes();
            }

            //if (!_event.Game.InstalledState.IsClient)
            //return true;

            await _dialogManager.MessageBox(
                new MessageBoxDialogParams(
                    "There appear to be updates available to install, you cannot join this server without installing them (configured by repo admin)"));
            return false;
        }

        async Task<bool> CheckServerSlots(Server server) {
            if (server.MaxPlayers <= 0 || server.FreeSlots >= _settings.ServerOptions.MinFreeSlots)
                return true;
            var result =
                await _dialogManager.MessageBox(new MessageBoxDialogParams(
                    $"The server appears to be at or near capacity, would you like to queue until at least {_settings.ServerOptions.MinFreeSlots} slots are available, or try joining anyway?",
                    server.FreeSlots == 0 ? "Server Full" : "Server Near Capacity", SixMessageBoxButton.YesNo) {
                        IgnoreContent = false,
                        GreenContent = "queue for server",
                        RedContent = "join anyway"
                    });
            switch (result) {
            case SixMessageBoxResult.Yes: {
                _event.Game.CalculatedSettings.Queued = server;
                return false;
            }
            case SixMessageBoxResult.Cancel:
                return false;
            }
            return true;
        }

        async Task<bool> CheckServerPassword(Server server) {
            if (!server.PasswordRequired ||
                (!String.IsNullOrWhiteSpace(server.SavedPassword) && server.SavePassword))
                return true;
            return await OpenPasswordDialog(server);
        }

        async Task<bool> CheckServerVersion(Server server) {
            var gameVer = _event.Game.InstalledState.Version;
            if (server.GameVer == null ||
                server.IsSameGameVersion(gameVer))
                return true;
            return (await _dialogManager.MessageBox(new MessageBoxDialogParams(
                $"The server appears to be running a different version of the Game ({server.GameVer} vs {gameVer}), do you wish to continue?\n\n(You could try 'Force enable beta patch' in the Game Options)", "Server appears to run another Game version", SixMessageBoxButton.YesNo))
                ).IsYes();
        }

        async Task<bool> OpenPasswordDialog(Server server) {
            Contract.Requires<ArgumentNullException>(server != null);
            var msg = $"Please enter Server Password for {server.Name}:";
            var defaultInput = server.SavedPassword;
            // Hopefully we are on a bg thread here or this fails
            var response = await _specialDialogManager.ShowEnterConfirmDialog(msg, defaultInput);
            if (response.Item1 == SixMessageBoxResult.Cancel)
                return false;

            var input = response.Item2 ?? String.Empty;
            if (response.Item1 == SixMessageBoxResult.YesRemember)
                server.SavePassword = true;
            else {
                server.SavedPassword = null;
                server.SavePassword = false;
            }
            server.SavedPassword = input.Trim();
            return true;
        }

        Task<SixMessageBoxResult> ShowPwsUriDialog(Uri pwsUri, string type) => _dialogManager.MessageBox(
        new MessageBoxDialogParams(
            "You are trying to join a server that appears to require a " + type +
            ", would you like to use it?\n"
            + pwsUri, "Use server custom repository?", SixMessageBoxButton.YesNoCancel));

        async Task<bool> HandlePwsUriDialogResult(Uri pwsUri, string type) {
            var result = await ShowPwsUriDialog(pwsUri, type).ConfigureAwait(false);
            if (result.IsNo())
                return false;
            if (result != SixMessageBoxResult.Cancel)
                ProcessPwsUri(pwsUri);
            return true;
        }

        static void ProcessPwsUri(Uri pwsUri) {
            Cheat.PublishEvent(new ProcessAppEvent(pwsUri.ToString()));
        }

        async Task<bool> CheckLaunchServer() {
            if (!await CheckServerVersion(_event.Server))
                return false;
            if (!await CheckServerPassword(_event.Server))
                return false;
            if (!await CheckServerSlots(_event.Server))
                return false;

            if (await ShouldProcessPwsCollectionUriOrCancel(_event.Server))
                return false;

            if (await ShouldProcessPwsUriOrCancel(_event.Server).ConfigureAwait(false))
                return false;

            return true;
        }

        async Task<bool> ShouldProcessPwsUriOrCancel(Server server) {
            var pwsUri = server.GetPwsUriFromName();
            if (pwsUri == null)
                return false;

            return !ActiveModSetMatchesPwsUri(pwsUri) && await HandlePwsUriDialogResult(pwsUri, "custom repository");
        }

        async Task<bool> ShouldProcessPwsCollectionUriOrCancel(Server server) {
            var pwsUri = server.GetPwsCollectionUriFromName();
            if (pwsUri == null)
                return false;

            return !ActiveModSetMatchesPwsCollectionUri(pwsUri) &&
                   await HandlePwsUriDialogResult(pwsUri, "shared collection");
        }

        bool ActiveModSetMatchesPwsUri(Uri pwsUri) {
            var customModSet = _event.Collection as CustomCollection;
            return customModSet != null &&
                   (customModSet.CustomRepo != null && customModSet.CustomRepoUrl == pwsUri.ToString());
        }

        bool ActiveModSetMatchesPwsCollectionUri(Uri pwsUri) {
            var getIdFromUrl = Guid.Empty; // TODO
            var customModSet = _event.Collection as CustomCollection;
            if (customModSet != null)
                return customModSet.PublishedId == getIdFromUrl;
            var subscribedModSet = _event.Collection as SubscribedCollection;
            if (subscribedModSet != null)
                return subscribedModSet.CollectionID == getIdFromUrl;

            return false;
        }

        async Task CheckRunningProcesses(string exeName) {
            var runningProcesses = RunningProcesses(exeName);
            if (runningProcesses.Any())
                await ConfirmRunningProcesses(exeName);
        }

        IEnumerable<Process> RunningProcesses(string gameExeName) => String.IsNullOrWhiteSpace(gameExeName)
    ? default(Process[])
    : Tools.Processes.FindProcess(gameExeName);

        string GetGameExeName() => _event.Game.InstalledState.Executable.FileName;

        async Task ConfirmRunningProcesses(string exeName) {
            if (!_settings.AppOptions.RememberWarnOnGameRunning) {
                var r =
                    await
                        _dialogManager.MessageBox(
                            new MessageBoxDialogParams(
                                "Game already appears to be running, would you like to close it before starting another instance?",
                                "Close Game before continuing?", SixMessageBoxButton.YesNo) {RememberedState = false})
                            .ConfigureAwait(false);

                switch (r) {
                case SixMessageBoxResult.YesRemember:
                    _settings.AppOptions.RememberWarnOnGameRunning = true;
                    _settings.AppOptions.WarnOnGameRunning = true;
                    break;
                case SixMessageBoxResult.NoRemember:
                    _settings.AppOptions.RememberWarnOnGameRunning = true;
                    _settings.AppOptions.WarnOnGameRunning = false;
                    break;
                }

                if (r.IsYes())
                    Tools.Processes.KillByName(exeName);
            } else {
                if (_settings.AppOptions.WarnOnGameRunning)
                    Tools.Processes.KillByName(exeName);
            }
        }
    }
}
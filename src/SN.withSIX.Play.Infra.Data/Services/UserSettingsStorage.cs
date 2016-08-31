// <copyright company="SIX Networks GmbH" file="UserSettingsStorage.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Core.Logging;
using SN.withSIX.Play.Applications.Services.Infrastructure;
using SN.withSIX.Play.Core;
using SN.withSIX.Play.Core.Options;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Play.Infra.Data.Services
{
    // TODO: Somehow this turned out as a stateful service? ;-P
    public class UserSettingsStorage : IEnableLogging, IInfrastructureService, IUserSettingsStorage
    {
        readonly Func<Exception, IAbsoluteFilePath, Task<bool>> _handleLoadFailure;
        readonly object _saveLock = new Object();
        CancellationTokenSource _cancellationTokenSource;
        IAbsoluteFilePath _currentVersionSettingsPath;
        Task _saveTask;
        IAbsoluteFilePath _settingsBasePath;
        int _tries;
        Version _version;
        // TODO: Handle failure differently
        public UserSettingsStorage(Func<Exception, IAbsoluteFilePath, Task<bool>> handleLoadFailure) {
            _handleLoadFailure = handleLoadFailure;
        }

        public UserSettings TryLoadSettings() {
            var settings = TryLoadSettingsInternal();
            settings.Changed.Subscribe(x => Save());
            return settings;
        }

        public void Save() {
            lock (_saveLock) {
                if (_saveTask != null && !_saveTask.IsCompleted)
                    return;
                HandleToken();
                _saveTask = SaveTask(_cancellationTokenSource.Token);
            }
        }

        public Task SaveNow() {
            lock (_saveLock) {
                HandleToken();
                return _saveTask = Task.Run(() => SaveNowInternal());
            }
        }

        public IEnumerable<Assembly> GetDiscoverableAssemblies() => AppDomain.CurrentDomain.GetAssemblies()
    .Where(x => !x.IsDynamic);

        UserSettings TryLoadSettingsInternal() {
            // TODO
            //if (Execute.InDesignMode)
            //    return new UserSettings();

            SetupPath();
            try {
                return Load();
            } catch (Exception e) {
                TryBackupSettings();
                if (_handleLoadFailure(e, _currentVersionSettingsPath).Result)
                    Environment.Exit(1);

                if (!_currentVersionSettingsPath.Exists || _tries > 0)
                    return new UserSettings();

                _tries++;
                Tools.FileUtil.Ops.DeleteIfExists(_currentVersionSettingsPath.ToString());
                return TryLoadSettings();
            }
        }

        void SetupPath() {
            Common.Paths.DataPath.MakeSurePathExists();
            _settingsBasePath = Common.Paths.DataPath.GetChildFileWithName("settings.xml");
            _version = CommonBase.AssemblyLoader.GetEntryVersion();
            _currentVersionSettingsPath = GetVersionedSettingsPath(_version);
        }

        void PrepareSettingsPath() {
            if (_currentVersionSettingsPath.Exists)
                return;

            MainLog.Logger.Info("Didn't find current version settings at: {0}, trying import of older version",
                Tools.FileUtil.FilterPath(_currentVersionSettingsPath));

            ImportOlderSettings();
        }

        void ImportOlderSettings() {
            var latestSettingsPath = GetLatestSettingsPath();
            if (latestSettingsPath.Exists) {
                MainLog.Logger.Info("Found {0}, will import settings", latestSettingsPath);
                Tools.FileUtil.Ops.CopyWithRetry(latestSettingsPath, _currentVersionSettingsPath);
                var latestSettingsFileName = latestSettingsPath.FileNameWithoutExtension.ToLower();
                if (latestSettingsFileName == "settings" ||
                    ParseSettingsFileVersion(latestSettingsFileName) < LegacySettingsConverter.LowestVersion) {
                    MainLog.Logger.Info("Convert legacy settings");
                    LegacySettingsConverter.ConvertLegacy(_currentVersionSettingsPath);
                }
                if (latestSettingsFileName == "settings" ||
                    ParseSettingsFileVersion(latestSettingsFileName) < LegacySettingsConverter.LowestVersion2) {
                    MainLog.Logger.Info("Convert legacy settings");
                    LegacySettingsConverter.ConvertLegacy2(_currentVersionSettingsPath);
                }
                return;
            }
            MainLog.Logger.Info("Found no existing settings to import, starting with default settings");
        }

        static Version ParseSettingsFileVersion(string settingsFileName) => settingsFileName.Replace("settings-", "").TryParseVersion();

        IAbsoluteFilePath GetLatestSettingsPath() {
            var versions =
                GetParsedVersions(Directory.EnumerateFiles(Common.Paths.DataPath.ToString(), "settings-*.xml"));
            if (!versions.Any())
                return _settingsBasePath;

            var compatibleVersion = versions.FirstOrDefault(version => version <= _version);
            return compatibleVersion == null ? _settingsBasePath : GetVersionedSettingsPath(compatibleVersion);
        }

        static IAbsoluteFilePath GetVersionedSettingsPath(Version version) => Common.Paths.DataPath.GetChildFileWithName(
            $"settings-{version.Major}.{version.Minor}.xml");

        static IOrderedEnumerable<Version> GetParsedVersions(IEnumerable<string> filePaths) => filePaths.Select(x => ParseSettingsFileVersion(Path.GetFileNameWithoutExtension(x)))
        .Where(x => x != null)
        .OrderByDescending(x => x);

        void TryBackupSettings() {
            try {
                BackupSettings();
            } catch (Exception e) {
                MainLog.Logger.FormattedWarnException(e);
            }
        }

        void BackupSettings() {
            Tools.FileUtil.Ops.CopyWithRetry(_currentVersionSettingsPath,
                (_currentVersionSettingsPath + ".bkp" + Tools.Generic.GetCurrentUtcDateTime.ToFileTime())
                    .ToAbsoluteFilePath());
        }

        void HandleToken() {
            var token = _cancellationTokenSource;
            if (token != null)
                token.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            if (token != null)
                token.Dispose();
        }

        async Task SaveTask(CancellationToken token) {
            await Task.Delay(1000*60, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return;

            await Task.Run(() => SaveNowInternal(), token).ConfigureAwait(false);
        }

        async Task SaveNowInternal() {
            this.Logger().Info("Saving Settings");
            lock (_saveLock)
                Tools.Serialization.Xml.SaveXmlToDiskThroughMemory(DomainEvilGlobal.Settings,
                    _currentVersionSettingsPath);
            await DomainEvilGlobal.SecretData.Save().ConfigureAwait(false);
        }

        UserSettings Load() {
            MigrateOldSettingsIfExists();
            PrepareSettingsPath();
            return Tools.FileUtil.Ops.AddIORetryDialog(() => TryLoadUserSettings(),
                _currentVersionSettingsPath.ToString());
        }

        UserSettings TryLoadUserSettings() {
            try {
                return
                    Tools.Serialization.Xml.LoadXmlFromFile<UserSettings>(_currentVersionSettingsPath.ToString()) ??
                    new UserSettings();
            } catch (FileNotFoundException) {
                return new UserSettings();
            }
        }

        void MigrateOldSettingsIfExists() {
            if (_settingsBasePath.Exists)
                return;
            var oldSettings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Play withSIX", "settings.xml");
            if (File.Exists(oldSettings))
                Tools.FileUtil.Ops.CopyWithRetry(oldSettings.ToAbsoluteFilePath(), _settingsBasePath);
        }

        class LegacySettingsConverter
        {
            public static readonly Version LowestVersion = new Version("1.50");
            public static readonly Version LowestVersion2 = new Version("1.68");

            public static void ConvertLegacy2(IAbsoluteFilePath settingsFile) {
                var data = File.ReadAllText(settingsFile.ToString());

                var newData = ProcessNamespaces2(data);

                File.WriteAllText(settingsFile.ToString(), newData);
            }

            static string ProcessNamespaces2(string data) => data
    .Replace("\"http://schemas.datacontract.org/2004/07/Six.Core.Domain",
        "\"http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core")
    .Replace("\"http://schemas.datacontract.org/2004/07/Six.Core",
        "\"http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core")
    .Replace("\"http://schemas.datacontract.org/2004/07/Six.Sync.Domain",
        "\"http://schemas.datacontract.org/2004/07/SN.withSIX.Sync.Core");

            public static void ConvertLegacy(IAbsoluteFilePath settingsFile) {
                var data = File.ReadAllText(settingsFile.ToString());

                var newData = ProcessLocalModFolders(data);
                newData = ProcessLocalMissionFolders(newData);
                newData = ProcessModSets(newData);

                File.WriteAllText(settingsFile.ToString(), newData);
            }

            static string ProcessLocalModFolders(string data) {
                var newData =
                    data.Replace(
                        "<_localMods xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\">",
                        "<_localMods xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models\">");
                newData =
                    newData.Replace(
                        "<a:anyType i:type=\"b:LocalMods\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models\">",
                        "<b:LocalMods>");
                newData = newData.Replace("</_gameUuid></a:anyType>", "</_gameUuid></b:LocalMods>");
                newData =
                    newData.Replace(
                        "<_gameUuid i:nil=\"true\" xmlns=\"http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Applications.Games.Legacy.Games\"/></a:anyType>",
                        "<_gameUuid i:nil=\"true\" xmlns=\"http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Applications.Games.Legacy.Games\"/></b:LocalMods>");
                newData = newData.Replace("<_gameUuid i:nil=\"true\"/></a:anyType>",
                    "<_gameUuid i:nil=\"true\"/></b:LocalMods>");
                return newData;
            }

            static string ProcessLocalMissionFolders(string data) {
                var newData =
                    data.Replace(
                        "<a:_localMissions xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\">",
                        "<a:_localMissions xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:c=\"http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models\">");
                newData =
                    newData.Replace(
                        "<b:anyType i:type=\"c:LocalMissions\" xmlns:c=\"http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models\">",
                        "<c:LocalMissions>");
                newData = newData.Replace("</b:anyType>", "</c:LocalMissions>");
                return newData;
            }

            static string ProcessModSets(string data) {
                var newData =
                    data.Replace(
                        "<_customModSets xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\">",
                        "<_customModSets xmlns:a=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models\">");
                newData =
                    newData.Replace(
                        "<a:anyType i:type=\"b:CustomModSet\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models\">",
                        "<b:CustomModSet>");
                newData = newData.Replace("</a:anyType>", "</b:CustomModSet>");
                return newData;
            }
        }
    }
}
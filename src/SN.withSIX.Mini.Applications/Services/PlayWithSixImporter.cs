// <copyright company="SIX Networks GmbH" file="PlayWithSixImporter.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Api.Models.Exceptions;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Mini.Applications.Services.Dtos;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Sync.Core.Legacy.Status;

namespace SN.withSIX.Mini.Applications.Services
{
    public interface IPlayWithSixImporter
    {
        IAbsoluteFilePath DetectPwSSettings();
        Task ImportPwsSettings(IAbsoluteFilePath filePath);
        Task<bool> ShouldImport();
    }

    // TODO: Import local mods??
    public class PlayWithSixImporter : IPlayWithSixImporter, IApplicationService
    {
        public const int ImportVersion = 1;
        readonly IDbContextLocator _locator;

        public PlayWithSixImporter(IDbContextLocator locator) {
            _locator = locator;
        }

        public IAbsoluteFilePath DetectPwSSettings() {
            var pwsPath = PathConfiguration.GetRoamingRootPath().GetChildDirectoryWithName("Play withSIX");
            return !pwsPath.Exists ? null : GetLatestSettingsPath(pwsPath);
        }

        public async Task<bool> ShouldImport() {
            var ctx = _locator.GetSettingsContext();
            var settings = await ctx.GetSettings().ConfigureAwait(false);
            return !settings.Local.DeclinedPlaywithSixImport &&
                   settings.Local.PlayWithSixImportVersion != ImportVersion;
        }

        public async Task ImportPwsSettings(IAbsoluteFilePath filePath) {
            try {
                await ImportPwsSettingsInternal(filePath).ConfigureAwait(false);
            } catch (Exception ex) {
                throw new ValidationException(
                    "A problem ocurred while trying to import settings from PwS. Please make sure the settings are of the latest PwS version",
                    ex);
            }
        }

        async Task ImportPwsSettingsInternal(IAbsoluteFilePath filePath) {
            var pwsSettings = filePath.LoadXml<UserSettings>();
            var db = _locator.GetGameContext();
            await db.LoadAll().ConfigureAwait(false);
            foreach (var g in db.Games) {
                var ss = pwsSettings.GameOptions.GameSettingsController.Profiles.FirstOrDefault()?.GameSettings;
                if (ss != null && ss.ContainsKey(g.Id))
                    HandleGameSettings(pwsSettings, g);
                HandleGameContent(pwsSettings, g);
            }

            // TODO
            var ctx = _locator.GetSettingsContext();
            var settings = await ctx.GetSettings().ConfigureAwait(false);
            settings.Local.PlayWithSixImportVersion = ImportVersion;
        }

        void HandleGameContent(UserSettings pwsSettings, Game game) {
            foreach (var c in pwsSettings.ModOptions.CustomModSets.Where(x => x.GameId == game.Id))
                ConvertToCollection(c, game);
            /*            foreach (var c in pwsSettings.ModOptions.Favorites) {
                            var existing = game.Collections.FirstOrDefault(x => x.Id == c.)
                        }*/
        }

        void ConvertToCollection(CustomCollection p0, Game game) {
            var isPublished = p0.PublishedId.HasValue;
            var exists = isPublished
                ? game.SubscribedCollections.Any(x => x.Id == p0.PublishedId.Value)
                : game.LocalCollections.Any(x => x.Id == p0.ID);
            if (exists || isPublished)
                return;
            var modNames = p0.AdditionalMods.Concat(p0.OptionalMods).Concat(p0.Mods).Where(x => x != null)
                .DistinctBy(x => x.ToLower());
            var packagedContents = game.Contents.OfType<IHavePackageName>();
            var contentDict = modNames.ToDictionary(x => x,
                x =>
                    packagedContents.FirstOrDefault(
                        c => c.PackageName.Equals(x, StringComparison.CurrentCultureIgnoreCase)) ??
                    CreateLocal(game, x));
            game.Contents.Add(new LocalCollection(game.Id, GetName(p0),
                contentDict.Values.Select(x => new ContentSpec((Content)x)).ToList()));
            // TODO: We should synchronize the network again before executing actions..
        }

        private static string GetName(SyncBase p0) => p0.Name ?? "Unnamed imported collection";

        static ModLocalContent CreateLocal(Game game, string x) {
            var modLocalContent = new ModLocalContent(x.ToLower(), game.Id, new BasicInstallInfo());
            game.Contents.Add(modLocalContent);
            return modLocalContent;
        }

        static void HandleGameSettings(UserSettings pwsSettings, Game g) {
            var gs = g.Settings as IHavePackageDirectory;
            var modDir = GetGameValue<string>(pwsSettings, g, "ModDirectory");
            if (gs != null && modDir != null)
                gs.PackageDirectory = modDir.ToAbsoluteDirectoryPath();
            var repoDir = GetGameValue<string>(pwsSettings, g, "RepositoryDirectory");
            if (repoDir != null)
                g.Settings.RepoDirectory = repoDir.ToAbsoluteDirectoryPath();
            var gameDir = GetGameValue<string>(pwsSettings, g, "Directory");
            if (gameDir != null)
                g.Settings.GameDirectory = gameDir.ToAbsoluteDirectoryPath();
            var startupLine = GetGameValue<string>(pwsSettings, g, "StartupLine");
            if (startupLine != null)
                g.Settings.StartupParameters.StartupLine = startupLine;
        }

        static T GetGameValue<T>(UserSettings pwsSettings, Game g, string propertyName)
            => pwsSettings.GameOptions.GameSettingsController.GetValue<T>(g.Id, propertyName);

        static IAbsoluteFilePath GetLatestSettingsPath(IAbsoluteDirectoryPath pwsPath) {
            var versions =
                GetParsedVersions(Directory.EnumerateFiles(pwsPath.ToString(), "settings-*.xml"));

            var compatibleVersion = versions.FirstOrDefault();
            return compatibleVersion == null ? null : GetVersionedSettingsPath(compatibleVersion, pwsPath);
        }

        static IOrderedEnumerable<Version> GetParsedVersions(IEnumerable<string> filePaths)
            => filePaths.Select(x => ParseSettingsFileVersion(Path.GetFileNameWithoutExtension(x)))
                .Where(x => x != null)
                .OrderByDescending(x => x);

        static Version ParseSettingsFileVersion(string settingsFileName)
            => settingsFileName.Replace("settings-", "").TryParseVersion();

        static IAbsoluteFilePath GetVersionedSettingsPath(Version version, IAbsoluteDirectoryPath pwsPath)
            => pwsPath.GetChildFileWithName(
                $"settings-{version.Major}.{version.Minor}.xml");
    }
}

namespace SN.withSIX.Mini.Applications.Services.Dtos
{
    [Obsolete("BWC for datacontract madness, and use IPEndpoint or so")]
    // This is IPEndPoint really?
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core")]
    public class ServerAddress : IEquatable<ServerAddress>
    {
        [DataMember] IPAddress _ip;
        [DataMember] int _port;
        string _stringFormat;

        public ServerAddress(string address) {
            var addrs = address.Split(':');
            if (addrs.Length < 2)
                throw new Exception("Invalid address format: " + address);

            var port = TryInt(addrs.Last());
            if (port < 1 || port > IPEndPoint.MaxPort)
                throw new ArgumentOutOfRangeException(port.ToString());

            _ip = IPAddress.Parse(string.Join(":", addrs.Take(addrs.Length - 1)));
            _port = port;
            _stringFormat = GetStringFormat();
        }

        public ServerAddress(string ip, int port) {
            if (string.IsNullOrWhiteSpace(ip))
                throw new ArgumentNullException(nameof(ip));
            if (port < 1 || port > IPEndPoint.MaxPort)
                throw new ArgumentOutOfRangeException(port.ToString());

            _ip = IPAddress.Parse(ip);
            _port = port;

            _stringFormat = GetStringFormat();
        }

        public ServerAddress(IPAddress ip, int port) {
            if (ip == null)
                throw new ArgumentNullException(nameof(ip));
            if (port < 1 || port > IPEndPoint.MaxPort)
                throw new ArgumentOutOfRangeException(port.ToString());

            _ip = ip;
            _port = port;

            _stringFormat = GetStringFormat();
        }

        public IPAddress IP => _ip;
        public int Port => _port;

        public bool Equals(ServerAddress other) {
            if (ReferenceEquals(null, other))
                return false;
            return ReferenceEquals(this, other) || string.Equals(_stringFormat, other._stringFormat);
        }

        string GetStringFormat() => $"{IP}:{Port}";

        public override int GetHashCode() => HashCode.Start.Hash(_stringFormat);

        static int TryInt(string val) {
            int result;
            return int.TryParse(val, out result) ? result : 0;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj.GetType() == GetType() && Equals((ServerAddress) obj);
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            if (_ip == null)
                throw new Exception("IP cant be null");
            if (_port < 1 || _port > IPEndPoint.MaxPort)
                throw new ArgumentOutOfRangeException(_port.ToString());

            _stringFormat = GetStringFormat();
        }

        public override string ToString() => _stringFormat;
    }


    [DataContract(Name = "UserSettings", Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core")
    ]
    public class UserSettings
    {
        [DataMember] AccountOptions _accountOptions = new AccountOptions();
        //[DataMember] AppOptions _appOptions = new AppOptions();
        //[DataMember] Version _appVersion;
        [DataMember] GameOptions _gameOptions = new GameOptions();
        //[DataMember] Migrations _migrations = new Migrations();
        //[DataMember] MissionOptions _missionOptions = new MissionOptions();
        [DataMember] ModOptions _modOptions = new ModOptions();
        public ModOptions ModOptions => _modOptions;
        public GameOptions GameOptions => _gameOptions;
        public AccountOptions AccountOptions => _accountOptions;
    }

    [DataContract(Name = "ModOptions", Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core")]
    public class ModOptions
    {
        [DataMember] List<CustomCollection> _customModSets = new List<CustomCollection>();
        [DataMember] List<FavoriteMod> _favoriteMods = new List<FavoriteMod>();
        [DataMember] List<FavoriteCollection> _favorites = new List<FavoriteCollection>();
        //[DataMember] List<LocalModsContainer> _localMods = new List<LocalModsContainer>();
        [DataMember] List<RecentCollection> _recentModSets = new List<RecentCollection>();
        [DataMember] List<SubscribedCollection> _subscribedModSets = new List<SubscribedCollection>();
        public List<CustomCollection> CustomModSets => _customModSets;
        public List<FavoriteMod> FavoriteMods => _favoriteMods;
        public List<FavoriteCollection> Favorites => _favorites;
        public List<RecentCollection> RecentModSets => _recentModSets;
        public List<SubscribedCollection> SubscribedModSets => _subscribedModSets;
    }

    [DataContract(Name = "AccountOptions",
        Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core")]
    public class AccountOptions {}

    [DataContract(Name = "RecentCollection",
        Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models")]
    public class RecentCollection {}

    [DataContract(Name = "FavoriteCollection",
        Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models")]
    public class FavoriteCollection {}

    [DataContract(Name = "FavoriteMod",
        Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models")]
    public class FavoriteMod {}

    [DataContract(Name = "SyncBase",
        Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models")]
    public abstract class SyncBase
    {
        [DataMember] Guid _id;
        [DataMember] string _Image;
        [DataMember] string _ImageLarge;
        [DataMember] string _Name;
        public Guid ID => _id;
        public string Name => _Name;
        public string ImageLarge => _ImageLarge;
        public string Image => _Image;
    }

    [DataContract(Name = "AdvancedCollection",
        Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models")]
    public abstract class AdvancedCollection : SyncBase
    {
        [DataMember] List<string> _additionalMods;
        [DataMember] protected List<string> _Mods;
        [DataMember] List<string> _optionalMods;
        [DataMember] Guid _realGameUuid;
        [DataMember] List<Uri> _repositories = new List<Uri>();
        [DataMember] List<CollectionServer> _servers = new List<CollectionServer>();
        public Guid GameId => _realGameUuid;
        public List<CollectionServer> Servers => _servers;
        public List<Uri> Repositories => _repositories;
        public List<string> OptionalMods => _optionalMods;
        public List<string> AdditionalMods => _additionalMods;
        public List<string> Mods => _Mods;
    }

    [DataContract(Name = "SubscribedCollection",
        Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models")]
    public class SubscribedCollection : AdvancedCollection
    {
        [DataMember] Guid _collectionID;
        public Guid CollectionID => _collectionID;
    }

    [DataContract(Name = "CustomModSet",
        Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models")]
    public class CustomCollection : AdvancedCollection
    {
        [DataMember] string _CustomRepoUrl;
        [DataMember] Guid? _publishedId;
        public string CustomRepoUrl => _CustomRepoUrl;
        public Guid? PublishedId => _publishedId;
    }

    [DataContract(Name = "CollectionServer",
        Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models")]
    public class CollectionServer
    {
        [DataMember]
        public ServerAddress Address { get; set; } // TODO: This might be problematic to import..
        [DataMember]
        public string Password { get; set; }
    }

    [DataContract(Name = "GameOptions", Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core")]
    public class GameOptions
    {
        [DataMember] GameSettingsController _gameSettingsController = new GameSettingsController();
        public GameSettingsController GameSettingsController => _gameSettingsController;
    }

    public interface IGetData
    {
        T GetData<T>(Guid gameId, string propertyName);
    }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core")]
    public enum ServerQueryMode
    {
        [EnumMember] All = 0,
        [EnumMember] Gamespy = 1,
        [EnumMember] Steam = 2
    }

    [DataContract(Name = "ServerFilter",
        Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Filters")]
    public class ArmaServerFilter {}

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core")]
    [KnownType(typeof (GlobalGameSettingsProfile)), KnownType(typeof (GameSettingsProfile)),
     KnownType(typeof (RecentGameSettings)), KnownType(typeof (ServerQueryMode)),
     KnownType(typeof (ProcessPriorityClass))]
    public class GameSettingsController
    {
        [DataMember] Guid? _activeProfileGuid;
        //List<GameSettings> _gameSettings = new List<GameSettings>();
        [DataMember] List<GameSettingsProfileBase> _profiles;
        public List<GameSettingsProfileBase> Profiles => _profiles;
        public Guid? ActiveProfileGuid => _activeProfileGuid;
        public GameSettingsProfileBase ActiveProfile { get; set; }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            ActiveProfile = _profiles.FirstOrDefault(x => x.Id == ActiveProfileGuid);
        }

        public T GetValue<T>(Guid game, string propertyName)
            => GetAllProfiles(ActiveProfile).Select(x => x.GetData<T>(game, propertyName))
                .FirstOrDefault(x => !EqualityComparer<T>.Default.Equals(x, default(T)));

        static IEnumerable<IGetData> GetAllProfiles(GameSettingsProfileBase activeProfile) {
            Contract.Requires<NullReferenceException>(activeProfile != null);

            var profile = activeProfile;
            while (profile != null) {
                yield return profile;
                profile = profile.Parent;
            }
        }
    }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core")]
    public class RecentGameSettings {}

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core")]
    [KnownType(typeof (ArmaServerFilter))]
    public abstract class GameSettingsProfileBase : PropertyChangedBase, IGetData
    {
        [DataMember] readonly Dictionary<Guid, ConcurrentDictionary<string, object>> _gameSettings =
            new Dictionary<Guid, ConcurrentDictionary<string, object>>();
        [DataMember] string _color;
        [DataMember] string _name;
        GameSettingsProfileBase _parent;

        public GameSettingsProfileBase(Guid id, string name, string color) : this(id) {
            _name = name;
            _color = color;
        }

        public GameSettingsProfileBase(Guid id) {
            Id = id;
        }

        public Dictionary<Guid, ConcurrentDictionary<string, object>> GameSettings => _gameSettings;
        [DataMember]
        public virtual Guid? ParentId { get; protected set; }
        [DataMember(Name = "Uuid")]
        public virtual Guid Id { get; }
        public virtual bool CanDelete => true;
        public virtual string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }
        public virtual string Color
        {
            get { return _color; }
            set { SetProperty(ref _color, value); }
        }
        public virtual GameSettingsProfileBase Parent
        {
            get { return _parent; }
            set
            {
                Contract.Requires<ArgumentNullException>(value != null);
                _parent = value;
            }
        }

        public T GetData<T>(Guid gameId, string propertyName) {
            var settings = _gameSettings[gameId];
            object propertyValue;
            settings.TryGetValue(propertyName, out propertyValue);
            return propertyValue == null ? default(T) : (T) propertyValue;
        }

        public bool SetData<T>(Guid gameId, string propertyName, T value) {
            var equalityComparer = EqualityComparer<T>.Default;
            if (equalityComparer.Equals(value, GetData<T>(gameId, propertyName)))
                return false;
            if (equalityComparer.Equals(value, default(T))) {
                object currentVal;
                _gameSettings[gameId].TryRemove(propertyName, out currentVal);
            } else
                _gameSettings[gameId][propertyName] = value;

            return true;
        }
    }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core")]
    public class GlobalGameSettingsProfile : GameSettingsProfileBase
    {
        public static readonly Guid GlobalId = new Guid("8b15f343-0ec6-4693-8b30-6508d6c44837");
        public GlobalGameSettingsProfile() : base(GlobalId) {}
        public override Guid Id => GlobalId;
        public override string Name
        {
            get { return "Global"; }
            set { }
        }
        public override string Color
        {
            get { return "#146bff"; }
            set { }
        }
        public override GameSettingsProfileBase Parent
        {
            get { return null; }
            set { }
        }
        public override bool CanDelete => false;
    }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core")]
    public class GameSettingsProfile : GameSettingsProfileBase
    {
        GameSettingsProfileBase _parent;

        protected GameSettingsProfile(Guid id, string name, string color, GameSettingsProfileBase parent)
            : base(id, name, color) {
            Contract.Requires<ArgumentNullException>(parent != null);
            _parent = parent;

            //SetupRefresh();
        }

        public GameSettingsProfile(string name, string color, GameSettingsProfileBase parent)
            : this(Guid.NewGuid(), name, color, parent) {}

        [DataMember(Name = "ParentUuid")]
        public override Guid? ParentId { get; protected set; }
        public override GameSettingsProfileBase Parent
        {
            get { return _parent; }
            set { SetProperty(ref _parent, value); }
        }
        /*
        void SetupRefresh() {
            this.WhenAnyValue(x => x.Parent)
                .Skip(1)
                .Subscribe(x => Refresh());
        }*/

        [OnSerializing]
        void OnSerializing(StreamingContext context) {
            ParentId = Parent?.Id;
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context) {
            //SetupRefresh();
        }
    }
}
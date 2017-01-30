// <copyright company="SIX Networks GmbH" file="ServerLibraryViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using ReactiveUI;
using ReactiveUI.Legacy;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.MVVM.Extensions;
using withSIX.Core.Applications.MVVM.Services;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;
using withSIX.Core.Logging;
using withSIX.Play.Applications.Extensions;
using withSIX.Play.Applications.Services;
using withSIX.Play.Core;
using withSIX.Play.Core.Games.Entities;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Games.Legacy.Servers;
using withSIX.Play.Core.Options;
using withSIX.Play.Core.Options.Filters;
using ReactiveCommand = ReactiveUI.Legacy.ReactiveCommand;

namespace withSIX.Play.Applications.ViewModels.Games.Library
{
    
    public class ServerLibraryViewModel :
        LibraryRootViewModel<ServerLibraryItemViewModel, Server, SearchServerLibraryItemViewModel>,
        IEnableLogging, ITransient
    {
        static readonly string[] defaultSplit = {" && ", "&&"};
        readonly IContentManager _contentList;
        readonly IDialogManager _dialogManager;
        private readonly ISpecialDialogManager _specialDialogManager;
        readonly LaunchManager _launchManager;
        readonly object _playersLock = new Object();
        readonly Lazy<ServersViewModel> _serversVm;
        ICollectionView _playersView;
        Server _selectedServer;

        public ServerLibraryViewModel(Game game, Lazy<ServersViewModel> serversVm, IContentManager contentList,
            IServerList serverList, LaunchManager launchManager, IDialogManager dialogManager, ISpecialDialogManager specialDialogManager) {
            _serversVm = serversVm;
            _contentList = contentList;
            Settings = DomainEvilGlobal.Settings;
            _launchManager = launchManager;
            ServerList = serverList;
            _dialogManager = dialogManager;
            _specialDialogManager = specialDialogManager;

            SearchItem = new SearchServerLibraryItemViewModel(this, new CleanServerFilter());

            this.SetCommand(x => x.JoinServ);
            JoinServ.RegisterAsyncTask(
                x => JoinServer((Server) x))
                .Subscribe();

            this.SetCommand(x => x.ServerInfoCommand).Cast<Server>().Subscribe(ServerInfo);
            UpdateCommand = UiTaskHandlerExtensions.CreateCommand("UpdateCommand", true, true);
            UpdateCommand.RegisterAsyncTaskVoid<Server>(UpdateServer).Subscribe();
            this.SetCommand(x => x.ResetFiltersCommand).Subscribe(RefreshFilter);
            this.SetCommand(x => x.Note).Cast<Server>().Subscribe(ShowNotes);

            ViewType = Settings.ServerOptions.ViewType;
            this.ObservableForProperty(x => x.ViewType)
                .Select(x => x.Value)
                .BindTo(Settings, s => s.ServerOptions.ViewType);

            this.WhenAnyValue(x => x.ActiveItem)
                .Skip(1)
                .Subscribe(x => game.CalculatedSettings.Server = x);

            DomainEvilGlobal.SelectedGame.WhenAnyValue(x => x.ActiveGame.CalculatedSettings.Server)
                .Subscribe(x => ActiveItem = x);

            var playerSortDescriptions =
                new[] {
                    new SortDescription("Score", ListSortDirection.Descending),
                    new SortDescription("Deaths", ListSortDirection.Ascending)
                };

            Players = new ReactiveList<Player>();
            Players.EnableCollectionSynchronization(_playersLock);
            PlayersView = Players.SetupDefaultCollectionView(playerSortDescriptions, null, null, null, true);

            this.WhenAnyValue(x => x.SelectedItem.SelectedItem)
                .OfType<Server>()
                .BindTo(this, x => x.SelectedServer);

            this.WhenAnyValue(x => x.SelectedServer.Players)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(UpdatePlayers);

            this.WhenAnyValue(x => x.SelectedItem.SelectedItem)
                .Subscribe(x => {
                    if (x == null)
                        Players.Clear();
                });

            IsLoading = true;
        }

        public Server SelectedServer
        {
            get { return _selectedServer; }
            private set { SetProperty(ref _selectedServer, value); }
        }
        public ReactiveCommand Note { get; private set; }
        public UserSettings Settings { get; }
        public ReactiveCommand UpdateCommand { get; }
        public ReactiveList<Player> Players { get; set; }
        public ICollectionView PlayersView
        {
            get { return _playersView; }
            protected set { SetProperty(ref _playersView, value); }
        }
        public IServerList ServerList { get; private set; }
        public ReactiveCommand JoinServ { get; private set; }
        public ReactiveCommand ServerInfoCommand { get; private set; }

        void RefreshFilter() {
            ServerList.Filter.ResetFilter();
        }

        void UpdatePlayers(Player[] x) {
            if (x == null) {
                Players.Clear();
                return;
            }
            x.SyncCollectionPK(Players);
        }

        public Task UpdateServer(Server server) => server.UpdateAsync();

        public void ServerInfo(Server server) {
            _serversVm.Value.ServerInfo(server);
        }

        public Task JoinServer(Server server) {
            ActiveItem = server;
            return _launchManager.JoinServer();
        }

        public void ShowNotes(Server server) {
            _serversVm.Value.ShowNotes(server);
        }

        public void ClearHistory(Server model) {
            Settings.ServerOptions.RemoveRecent(model);
        }


        public void CopyIpPortAction(object x) {
            Clipboard.SetText(((Server) x).ServerAddress.ToString());
        }


        public void CopyDetailsAction(object x) {
            Clipboard.SetText(((Server) x).Details());
        }


        public Task<bool> ChangePassword(Server server) => OpenPasswordDialog(server);

        async Task<bool> OpenPasswordDialog(Server server) {
            if (server == null) throw new ArgumentNullException(nameof(server));
            var msg = $"Please enter Server Password for {server.Name}:";
            var defaultInput = server.SavedPassword;

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

        protected override bool ApplySearchFilter(object obj) {
            var content = obj as Server;
            var searchText = SearchText;
            return content != null
                   && (MatchName(content, searchText)
                       || MatchMod(content, searchText)
                       || MatchMission(content, searchText)
                       || MatchIsland(content, searchText)
                       || MatchPlayer(content, searchText));
        }

        static bool MatchName(Server server, string searchText) => AdvancedStringSearch(searchText, "",
    normalTestFunc: search => server.Name.NullSafeContainsIgnoreCase(searchText),
    reverseTestFunc: search => server.Name.NullSafeContainsIgnoreCase(searchText)); // TODO: hmmm

        static bool MatchPlayer(Server server, string searchText) => AdvancedStringSearch(searchText, "",
    normalTestFunc:
        search =>
            server.Players != null && !server.Players.None(x => x.Name.NullSafeContainsIgnoreCase(search)),
    reverseTestFunc: search => server.Players.Any(x => x.Name.NullSafeContainsIgnoreCase(search)));

        static bool MatchMod(Server server, string searchText) => AdvancedStringSearch(searchText, "",
    normalTestFunc: search => server.Mods != null && !server.Mods.None(x => x.ContainsIgnoreCase(search)),
    reverseTestFunc: search => server.Mods.Any(x => x.ContainsIgnoreCase(search)));

        static bool MatchMission(Server server, string searchText) => AdvancedStringSearch(searchText, "",
    normalTestFunc: search => server.Mission.NullSafeContainsIgnoreCase(searchText),
    reverseTestFunc: search => server.Mission.NullSafeContainsIgnoreCase(searchText));

        static bool MatchIsland(Server server, string searchText) => AdvancedStringSearch(searchText, "",
    normalTestFunc: search => server.Island.NullSafeContainsIgnoreCase(searchText),
    reverseTestFunc: search => server.Island.NullSafeContainsIgnoreCase(searchText));

        static bool AdvancedStringSearch(string searchField, string queryField, string[] split = null,
            Func<string, bool> normalTestFunc = null, Func<string, bool> reverseTestFunc = null) {
            if (split == null)
                split = defaultSplit;

            var searchStrings = searchField.Split(split, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in searchStrings) {
                var reverse = item.Substring(0, 1) == "!";
                if (!reverse) {
                    if (normalTestFunc != null) {
                        if (!normalTestFunc(item))
                            return false;
                    } else if (!queryField.NullSafeContainsIgnoreCase(item))
                        return false;
                } else {
                    if (reverseTestFunc != null) {
                        if (reverseTestFunc(item.Substring(1)))
                            return false;
                    } else if (queryField.NullSafeContainsIgnoreCase(item.Substring(1)))
                        return false;
                }
            }
            return true;
        }

        public override sealed void Setup() {
            ServerList = _serversVm.Value.ServerList;
            var current = SetUp as ServerLibrarySetup;
            SetUp = new ServerLibrarySetup(this, ServerList);
            if (current != null)
                current.Dispose();
            InitialSelectedItem();
            IsLoading = false;
        }

        protected override void InitialSelectedItem() {
            SelectedItem = SetUp.Items.FirstOrDefault();
        }

        
        public void ActivateItem(Server server) {
            SelectedItem.SelectedItem = server;
            ActiveItem = server;
        }

        public void CreateCollectionFromServer(Server entity) {
            _contentList.SelectCollection(_contentList.CreateAndAddCustomModSet(entity));
        }

        public override Task RemoveLibraryItem(ContentLibraryItemViewModel contentLibraryItemViewModel) {
            throw new NotImplementedException();
        }
    }
}
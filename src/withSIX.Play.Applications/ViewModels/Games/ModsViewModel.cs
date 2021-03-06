// <copyright company="SIX Networks GmbH" file="ModsViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using NDepend.Path;
using ReactiveUI;


using withSIX.Core;
using withSIX.Core.Applications.Extensions;
using withSIX.Core.Applications.MVVM;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;
using withSIX.Play.Applications.Services;
using withSIX.Play.Applications.ViewModels.Games.Library;
using withSIX.Play.Applications.ViewModels.Games.Overlays;
using withSIX.Play.Applications.ViewModels.Games.Popups;
using withSIX.Play.Core;
using withSIX.Play.Core.Games.Entities;
using withSIX.Play.Core.Games.Legacy;
using withSIX.Play.Core.Games.Legacy.Events;
using withSIX.Play.Core.Games.Legacy.Helpers;
using withSIX.Play.Core.Games.Legacy.Mods;
using withSIX.Play.Core.Options;

namespace withSIX.Play.Applications.ViewModels.Games
{
    
    public class ModsViewModel : LibraryModuleViewModel, IHandle<SubGamesChanged>,
        IHandle<GameContentInitialSynced>, IHandle<ActiveGameChangedForReal>
    {
        readonly Lazy<IContentManager> _contentManager;
        private readonly ISpecialDialogManager _specialDialogManager;
        readonly IDialogManager _dialogManager;
        readonly IViewModelFactory _factory;
        readonly object _libLock = new object();
        readonly Lazy<IUpdateManager> _updateManager;
        ModLibraryViewModel _libraryVm;
        protected ModsViewModel() {}

        public ModsViewModel(ModSettingsOverlayViewModel msovm,
            ModInfoOverlayViewModel miovm, ModVersionOverlayViewModel mvovm, IDialogManager dialogManager,
            Lazy<IUpdateManager> updateManager, IViewModelFactory factory,
            Lazy<IContentManager> contentManager, UserSettings settings, ISpecialDialogManager specialDialogManager) {
            _updateManager = updateManager;
            _contentManager = contentManager;
            _specialDialogManager = specialDialogManager;
            _dialogManager = dialogManager;
            _factory = factory;
            UserSettings = settings;

            ModuleName = ControllerModules.ModBrowser;
            DisplayName = "Mods";
            ModSetSettingsOverlay = msovm;
            ModSetInfoOverlay = miovm;
            ModSetVersionOverlay = mvovm;
        }

        public UserSettings UserSettings { get; }
        public ModLibraryViewModel LibraryVM
        {
            get { return _libraryVm; }
            protected set { SetProperty(ref _libraryVm, value); }
        }
        public ModSettingsOverlayViewModel ModSetSettingsOverlay { get; protected set; }
        public ModInfoOverlayViewModel ModSetInfoOverlay { get; protected set; }
        public ModInfoOverlayViewModel ModSetVersionOverlay { get; protected set; }
        public bool LockDown => Common.Flags.LockDown;
        public IContentManager ContentManager => _contentManager.Value;

        protected override void OnInitialize() {
            base.OnInitialize();

            this.WhenAnyValue(x => x.ModSetSettingsOverlay.IsActive)
                .Where(x => x).Subscribe(x => ModSetInfoOverlay.TryClose());

            this.WhenAnyValue(x => x.ModSetInfoOverlay.IsActive)
                .Where(x => x).Subscribe(x => ModSetSettingsOverlay.TryClose());

            this.WhenAnyValue(x => x.LibraryVM.IsLoading).Subscribe(x => ProgressState.Active = x);

            this.WhenAnyValue(x => x.LibraryVM.SelectedItem.SelectedItem)
                .OfType<IMod>()
                .Where(x => x != null)
                .Select(x => new {x, Game = DomainEvilGlobal.SelectedGame.ActiveGame as ISupportModding})
                .Where(x => x.Game != null)
                .Subscribe(x => x.x.LoadSettings(x.Game));

            DomainEvilGlobal.SelectedGame.WhenAnyValue(x => x.ActiveGame)
                .Subscribe(ReplaceLibraryIfRequired);
        }

        void ReplaceLibraryIfRequired(Game x) {
            lock (_libLock) {
                if (LibraryVM == null || LibraryVM.Game != x)
                    CreateLibrary(x);
            }
        }

        public IMod GetSelectedMod() {
            var modLib = LibraryVM;
            if (modLib == null)
                return null;
            var selected = modLib.SelectedItem;
            if (selected == null)
                return null;
            var content = selected.SelectedItem;
            return content != null ? content.ToMod() : null;
        }

        void CreateLibrary(Game game) {
            if (game.SupportsMods()) {
                UiHelper.TryOnUiThread(
                    () => {
                        var modLibraryViewModel = _factory.CreateModLibraryViewModel(game).Value;
                        ((IActivate) modLibraryViewModel).Activate();
                        LibraryVM = modLibraryViewModel;
                    });
            } else
                LibraryVM = null;
        }

        public Task<CustomCollection> CreateModSet(IContent content = null) => Task.Run(() => _contentManager.Value.CreateAndSelectCustomModSet(content));

        public Task<CustomCollection> CreateModSet(IReadOnlyCollection<IContent> content) => Task.Run(() => _contentManager.Value.CreateAndSelectCustomModSet(content));

        public Task ShowAddRepository() => _specialDialogManager.ShowPopup(_factory.CreateAddRepository().Value);

        public Task ShowCreateCollection() => _specialDialogManager.ShowPopup(new CreateCollectionViewModel(_contentManager.Value));

        public void SelectNoModSet() {
            LibraryVM.ActiveItem = null;
        }


        public void ModSetConfigure(object x) {
            ShowOverlay(ModSetSettingsOverlay);
        }


        public void ModSetInfo() {
            ShowOverlay(ModSetInfoOverlay);
        }


        public void ModVersion() {
            ShowOverlay(ModSetVersionOverlay);
        }


        public async Task ModUninstall(IMod mod) {
            if (mod == null) throw new ArgumentNullException(nameof(mod));

            LibraryVM.ActiveItem = mod;

            var report =
                !(await _dialogManager.MessageBox(
                    new MessageBoxDialogParams($"You are about to uninstall {mod.Name}\nare you sure?", "Are you sure you want to uninstall the Modification?", SixMessageBoxButton.YesNo) {
                            IgnoreContent = false,
                            GreenContent = "delete",
                            RedContent = "keep"
                        })).IsYes();

            if (report)
                return;

            try {
                await _updateManager.Value.HandleUninstall().ConfigureAwait(false);
            } catch (BusyStateHandler.BusyException) {
                _dialogManager.BusyDialog();
            }
        }

        public async Task UninstallModSet(Collection collection) {
            if (collection == null)
                return;

            LibraryVM.ActiveItem = collection;

            var report =
                !(await _dialogManager.MessageBox(
                    new MessageBoxDialogParams(
                        $"You are about to uninstall {collection.Name}\n{String.Join(", ", collection.Items.Select(x => x.Name).ToArray())}\nare you sure?",
                        "Are you sure you want to uninstall the ModSet and all its modfolders?",
                        SixMessageBoxButton.YesNo) {
                            IgnoreContent = false,
                            GreenContent = "delete",
                            RedContent = "keep"
                        })).IsYes();

            if (report)
                return;

            await TryUninstallModSet(collection).ConfigureAwait(false);
        }

        async Task TryUninstallModSet(Collection collection) {
            try {
                await _updateManager.Value.HandleUninstall().ConfigureAwait(false);
            } catch (BusyStateHandler.BusyException) {
                _dialogManager.BusyDialog();
            } finally {
                var custom = collection as CustomCollection;
                if (custom != null)
                    _contentManager.Value.RemoveCollection(custom);
            }
        }

        public void ViewSupportAction(Collection x) {
            Tools.Generic.TryOpenUrl(x.SupportUrl);
        }

        public void ViewHomepageAction(IContent x) {
            Tools.Generic.TryOpenUrl(x.HomepageUrl);
        }

        public void ViewChangelogAction(IContent x) {
            Tools.Generic.TryOpenUrl(x.GetChangelogUrl());
        }


        
        public void OpenNote(IHaveNotes x) {
            ShowNotes(x);
        }

        public Task<IAbsoluteFilePath> CreateIcon(Collection collection) => _contentManager.Value.CreateIcon(collection);

        public void SwitchGame(Game game) {
            ReplaceLibraryIfRequired(game);
        }

        #region IHandle events

        public void Handle(ActiveGameChangedForReal message) {
            if (message.Game.SupportsMods())
                LibraryVM.Setup();
        }

        public void Handle(GameContentInitialSynced message) {
            LibraryVM?.Setup();
        }

        public void Handle(SubGamesChanged message) {
            var libvm = LibraryVM;
            if (libvm != null && libvm.Game == message.Game)
                libvm.Reset();
        }

        #endregion
    }
}
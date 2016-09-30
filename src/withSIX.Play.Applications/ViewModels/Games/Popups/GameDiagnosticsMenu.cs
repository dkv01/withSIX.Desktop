// <copyright company="SIX Networks GmbH" file="GameDiagnosticsMenu.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using withSIX.Core.Applications.MVVM.Attributes;
using withSIX.Core.Applications.MVVM.ViewModels;
using withSIX.Core.Applications.Services;
using withSIX.Play.Applications.ViewModels.Games.Dialogs;

namespace withSIX.Play.Applications.ViewModels.Games.Popups
{
    public class GameDiagnosticsMenu : ContextMenuBase
    {
        readonly IDialogManager _dialogManager;
        private readonly ISpecialDialogManager _specialDialogManager;

        public GameDiagnosticsMenu(IDialogManager dialogManager, ISpecialDialogManager specialDialogManager) {
            _dialogManager = dialogManager;
            _specialDialogManager = specialDialogManager;
        }

        [MenuItem]
        public Task DiagnoseAndRepairSynqRepository() {
            var vm = new RepairViewModel();
            vm.ProcessCommand.Execute(null);
            return _specialDialogManager.ShowDialog(vm);
        }
    }
}
// <copyright company="SIX Networks GmbH" file="NotesViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using ReactiveUI.Legacy;

using withSIX.Core.Applications.MVVM.Services;
using withSIX.Core.Applications.Services;
using withSIX.Play.Applications.ViewModels.Overlays;
using withSIX.Play.Core;

namespace withSIX.Play.Applications.ViewModels.Games.Overlays
{
    
    public class NotesViewModel : OverlayViewModelBase
    {
        IHaveNotes _item;

        public NotesViewModel(IHaveNotes item) {
            Item = item;
            DisplayName = "Notes";
            this.SetCommand(x => x.CloseNoteCommand).Subscribe(CloseNote);
        }

        public ReactiveCommand CloseNoteCommand { get; private set; }
        public IHaveNotes Item
        {
            get { return _item; }
            set { SetProperty(ref _item, value); }
        }


        void CloseNote(object x) {
            TryClose(true);
        }
    }
}
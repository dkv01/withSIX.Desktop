// <copyright company="SIX Networks GmbH" file="ScreenBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using Caliburn.Micro;
using withSIX.Core.Helpers;

namespace withSIX.Core.Applications.MVVM.ViewModels
{
    // Using both ReactiveUI and Caliburn.Micro


    public abstract class ScreenBase : ReactiveScreen, IViewModel {}


    public abstract class ScreenBase<T> : ScreenBase where T : class
    {
        protected T ParentShell => Parent as T;
    }

    public abstract class ReactiveModalScreen<T> : ScreenBase<T>, IModalScreen where T : class, IModalShell
    {
        protected ReactiveModalScreen() {
            ShowBackButton = true;
        }


        public virtual void Cancel() {
            TryClose(false);
        }

        public bool ShowBackButton { get; protected set; }


        public override void TryClose(bool? dialogResult) {
            if (ParentShell == null)
                return;
            ((IDeactivate) this).Deactivate(true);
            ParentShell.HideModalView();
        }
    }

    public class ReactiveModalScreen<T, T2> : ReactiveModalScreen<T>, IHaveModel<T2>
        where T : class, IModalShell
        where T2 : class
    {
        protected static readonly string ModelProperty = "Model";

        public ReactiveModalScreen(T2 model) {
            if (model == null) throw new ArgumentNullException(nameof(model));
            Model = model;
        }

        public T2 Model { get; }
    }
}
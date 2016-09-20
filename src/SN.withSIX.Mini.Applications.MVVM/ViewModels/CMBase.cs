// <copyright company="SIX Networks GmbH" file="CMBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using ReactiveUI;
using SN.withSIX.Core.Applications.MVVM.ViewModels;
using SN.withSIX.Core.Applications.Services;

namespace SN.withSIX.Mini.Applications.MVVM.ViewModels
{
    public abstract class CMBase : ContextMenuBase, IUsecaseExecutor //, ISupportsActivation
    {
        /*
        protected CMBase() {
            Activator = new ViewModelActivator();
            // TODO: Activation doesnt work atm? :S
            this.WhenAnyValue(x => x.IsOpen)
                .Subscribe(x => {
                    if (x)
                        using (Activator.Activate())
                            ;
                    else
                        Activator.Deactivate();
                });
        }
        */

        public ViewModelActivator Activator { get; }
    }
}
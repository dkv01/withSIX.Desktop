﻿// <copyright company="SIX Networks GmbH" file="ViewModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using ReactiveUI;
using withSIX.Core.Applications.Services;

namespace withSIX.Mini.Applications.MVVM.ViewModels
{
    public abstract class ViewModel : ReactiveObject, IViewModel, ISupportsActivation
    {
        protected ViewModel() {
            Activator = new ViewModelActivator();
        }

        public ViewModelActivator Activator { get; }
    }

    public interface IViewModel : IReactiveObject, IUsecaseExecutor {}
}
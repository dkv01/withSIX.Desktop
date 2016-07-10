// <copyright company="SIX Networks GmbH" file="ISingleInstanceApp.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;


namespace SN.withSIX.Core.Applications.MVVM.ViewModels
{
    
    public interface ISingleInstanceApp
    {
        IList<string> LastAppEvent { get; set; }
        bool SignalExternalCommandLineArgs(IList<string> args);
        event EventHandler<IList<string>> AppEvent;
        event EventHandler Activated;
        event EventHandler Deactivated;
    }
}
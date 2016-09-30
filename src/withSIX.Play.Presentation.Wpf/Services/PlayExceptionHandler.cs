// <copyright company="SIX Networks GmbH" file="PlayExceptionHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using ReactiveUI;
using withSIX.Core.Applications.Errors;
using withSIX.Core.Presentation.Services;
using withSIX.Play.Core.Connect;
using withSIX.Play.Core.Games.Legacy;

namespace withSIX.Play.Presentation.Wpf.Services
{
    public class PlayExceptionHandler : DefaultExceptionHandler
    {
        public PlayExceptionHandler(IEnumerable<IHandleExceptionPlugin> ehs) : base(ehs) {}

        protected override UserError HandleExceptionInternal(Exception ex, string action = "Action") {
            Contract.Requires<ArgumentNullException>(action != null);

            if (ex is NotConnectedException)
                return new NotConnectedUserError(innerException: ex);

            if (ex is NotLoggedInException)
                return new NotLoggedInUserError(innerException: ex);
            if (ex is BusyStateHandler.BusyException)
                return new BusyUserError(innerException: ex);

            return Handle(ex, action);
        }
    }
}
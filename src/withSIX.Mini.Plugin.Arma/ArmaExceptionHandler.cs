﻿// <copyright company="SIX Networks GmbH" file="ArmaExceptionHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using withSIX.Core.Applications.Errors;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications;
using withSIX.Mini.Plugin.Arma.Models;

namespace withSIX.Mini.Plugin.Arma
{
    public class ArmaExceptionHandler : BasicExternalExceptionhandler, IUsecaseExecutor
    {
        public override UserErrorModel HandleException(Exception ex, string action = "Action") {
            if (action == null) throw new ArgumentNullException(nameof(action));
            return Handle((dynamic) ex, action);
        }

        protected static RecoverableUserError Handle(ParFileException ex, string action)
            =>
            new RecoverableUserError(ex, "Please make sure the PAR path is writable",
                "Can't write the startup parameter file to start the game, make sure it's not running and that the path is writable\n\nError info: " +
                ex.Message);
    }
}
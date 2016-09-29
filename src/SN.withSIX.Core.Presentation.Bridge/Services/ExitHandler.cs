// <copyright company="SIX Networks GmbH" file="ExitHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Core.Presentation.Services;

namespace withSIX.Core.Presentation.Bridge.Services
{
    public class ExitHandler : IExitHandler, IPresentationService
    {
        public void Exit(int exitCode = 0) {
            Environment.Exit(exitCode);
        }
    }
}
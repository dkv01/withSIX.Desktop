// <copyright company="SIX Networks GmbH" file="ShutdownHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Core.Applications.Services;

namespace withSIX.Mini.Presentation.CoreCore.Services
{
    public class ShutdownHandler : ExitHandler, IShutdownHandler
    {
        public void Shutdown(int exitCode = 0) {
            Exit(exitCode);
        }
    }
}
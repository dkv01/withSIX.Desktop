// <copyright company="SIX Networks GmbH" file="ShutdownHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Presentation.Bridge.Services;

namespace SN.withSIX.Core.Presentation.Bridge
{
    public class ShutdownHandler : ExitHandler, IShutdownHandler
    {
        public void Shutdown(int exitCode = 0) {
            Exit(exitCode);
        }
    }
}
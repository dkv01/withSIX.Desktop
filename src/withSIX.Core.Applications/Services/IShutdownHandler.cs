// <copyright company="SIX Networks GmbH" file="IShutdownHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Core.Applications.Services
{
    public interface IShutdownHandler
    {
        void Shutdown(int exitCode = 0);
    }
}
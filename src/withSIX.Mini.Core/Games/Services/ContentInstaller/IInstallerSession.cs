// <copyright company="SIX Networks GmbH" file="IInstallerSession.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace withSIX.Mini.Core.Games.Services.ContentInstaller
{
    public interface IInstallerSession
    {
        Task Install(IReadOnlyCollection<IContentSpec<IPackagedContent>> content);
        Task Synchronize();
        Task RunCE(IPackagedContent content);
        void Activate(IInstallContentAction<IInstallableContent> action, Func<ProgressInfo, Task> statusChange);
    }
}
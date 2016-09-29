// <copyright company="SIX Networks GmbH" file="IExplorerExtensionInstaller.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Mini.Applications.Models;

namespace withSIX.Mini.Applications.Services
{
    public interface IExplorerExtensionInstaller
    {
        Task UpgradeOrInstall(IAbsoluteDirectoryPath destination, Settings settings, params IAbsoluteFilePath[] files);
        Task Install(IAbsoluteDirectoryPath destination, Settings settings, params IAbsoluteFilePath[] files);
        Task Uninstall(IAbsoluteDirectoryPath destination, Settings settings, params IAbsoluteFilePath[] files);
    }
}
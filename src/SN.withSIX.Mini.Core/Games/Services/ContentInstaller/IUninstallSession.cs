// <copyright company="SIX Networks GmbH" file="IUninstallSession.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

namespace SN.withSIX.Mini.Core.Games.Services.ContentInstaller
{
    public interface IUninstallSession
    {
        Task Uninstall();
        Task Uninstall<T>(T content) where T : Content, IContentWithPackageName;
        Task UninstallCollection(Collection collection, CancellationToken cancelToken, string constraint = null);
    }
}
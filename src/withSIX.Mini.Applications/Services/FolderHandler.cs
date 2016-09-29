// <copyright company="SIX Networks GmbH" file="FolderHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Core.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Services
{
    public class FolderHandler : IApplicationService, IFolderHandler
    {
        readonly List<IAbsoluteDirectoryPath> _folders = new List<IAbsoluteDirectoryPath>();
        private readonly IDbContextLocator _locator;

        public FolderHandler(IDbContextLocator locator) {
            _locator = locator;
        }

        public void WhiteListFolder(IAbsoluteDirectoryPath path) {
            if (!_folders.Any(path.Equals))
                _folders.Add(path);
        }

        public async Task<bool> IsFolderWhitelisted(IAbsoluteDirectoryPath path) {
            if (_folders.Any(path.Equals))
                return true;
            var ctx = _locator.GetContentLinkContext();
            return (await ctx.GetFolderLink().ConfigureAwait(false)).Infos.Any(x => path.Equals(x.Path));
        }
    }

    public interface IFolderHandler
    {
        void WhiteListFolder(IAbsoluteDirectoryPath path);
        Task<bool> IsFolderWhitelisted(IAbsoluteDirectoryPath path);
    }
}
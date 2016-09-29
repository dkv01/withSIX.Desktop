// <copyright company="SIX Networks GmbH" file="IContentFolderLinkContext.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;
using withSIX.Core.Applications.Infrastructure;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Applications.Services.Infra
{
    public interface IContentFolderLinkContext : IUnitOfWork
    {
        Task<ContentFolderLink> GetFolderLink();
    }
}
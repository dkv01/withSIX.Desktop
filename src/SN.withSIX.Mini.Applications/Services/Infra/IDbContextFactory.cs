// <copyright company="SIX Networks GmbH" file="IDbContextFactory.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;

namespace SN.withSIX.Mini.Applications.Services.Infra
{
    public interface IDbContextFactory
    {
        IDbContextScope Create();
        IDisposable SuppressAmbientContext();
    }

    public interface IDbContextLocator
    {
        IGameContext GetGameContext();
        IContentFolderLinkContext GetContentLinkContext();
        IGameContextReadOnly GetReadOnlyGameContext();
        ISettingsStorage GetSettingsContext();
        ISettingsStorageReadOnly GetReadOnlySettingsContext();
    }

    public interface IDbContextScope : IDisposable
    {
        void SaveChanges();
        Task SaveChangesAsync();
    }
}
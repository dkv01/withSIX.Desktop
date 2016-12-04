// <copyright company="SIX Networks GmbH" file="DbCommandBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using withSIX.Mini.Applications.Services.Infra;

namespace withSIX.Mini.Applications.Features
{
    public abstract class DbRequestBase
    {
        protected readonly IDbContextLocator DbContextLocator;

        protected DbRequestBase(IDbContextLocator dbContextLocator) {
            DbContextLocator = dbContextLocator;
        }
    }

    public abstract class DbCommandBase : DbRequestBase
    {
        protected DbCommandBase(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}
        protected IGameContext GameContext => DbContextLocator.GetGameContext();
        protected IContentFolderLinkContext ContentLinkContext => DbContextLocator.GetContentLinkContext();
        protected ISettingsStorage SettingsContext => DbContextLocator.GetSettingsContext();
    }

    public abstract class DbQueryBase : DbRequestBase
    {
        protected DbQueryBase(IDbContextLocator dbContextLocator) : base(dbContextLocator) {}
        protected IGameContextReadOnly GameContext => DbContextLocator.GetReadOnlyGameContext();
        protected IContentFolderLinkContext ContentLinkContext => DbContextLocator.GetContentLinkContext();
        protected ISettingsStorageReadOnly SettingsContext => DbContextLocator.GetReadOnlySettingsContext();
    }
}
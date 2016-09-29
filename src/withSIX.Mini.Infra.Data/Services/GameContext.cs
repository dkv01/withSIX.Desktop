// <copyright company="SIX Networks GmbH" file="GameContext.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Infra.Data.Services
{
    // TODO: Consider to use an alternative underlying storage mechanism: Key Value, so we can ignore games who's plugins are currently not installed!

    // What about data migration over time? Best to go Entity Framework directly with migrations, or stick to Akavache basic K/V store and do manual plumbing??
    // TODO: Future - Upgrade to a system where we are not serializing/deserializing the whole data store at once ;-)
    public abstract class GameContext : ContextBase, IGameContext
    {
        public virtual ICollection<Game> Games { get; protected set; } = new List<Game>();
        //public virtual ICollection<Group> Groups { get; protected set; } = new List<Group>();
        public abstract Task Load(Guid gameId);
        public abstract Task LoadAll(bool skip = false);
        public abstract Task<bool> GameExists(Guid gameId);
        // Instead we use Games as the aggregate root and therefore also spare us some Persistence plumbing right now...
        //        public virtual ICollection<Content> Contents { get; } = new List<Content>();
        //        public virtual ICollection<RecentItem> Recents { get; } = new List<RecentItem>();

        public abstract Task<bool> Migrate(List<Migration> migrations);
        // We expect a convention where the settings exist in the same namespace as the game, and are {GameClassName}Settings
    }
}
// <copyright company="SIX Networks GmbH" file="IHaveCustomRepo.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using withSIX.Play.Core.Games.Legacy.Repo;

namespace withSIX.Play.Core.Games.Legacy.Mods
{
    public interface IHaveCustomRepo
    {
        IReadOnlyCollection<CustomRepoMod> CustomRepoMods { get; set; }
        SixRepo CustomRepo { get; set; }
        string CustomRepoUrl { get; set; }
        string CustomRepoUuid { get; set; }
        SixRepoApp[] CustomRepoApps { get; set; }
    }
}
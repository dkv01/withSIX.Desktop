// <copyright company="SIX Networks GmbH" file="CustomRepoMod.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

using withSIX.Core.Extensions;

namespace withSIX.Play.Core.Games.Legacy.Mods
{
    
    public class CustomRepoMod : Mod
    {
        public CustomRepoMod(Guid id) : base(id) {}
        public override bool IsCustomContent => true;

        public override string GetRemotePath() => Name;

        protected override string GetSlugType() => typeof(Mod).Name.ToUnderscore() + "s";
    }
}
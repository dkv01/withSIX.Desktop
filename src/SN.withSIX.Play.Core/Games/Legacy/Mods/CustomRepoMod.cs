// <copyright company="SIX Networks GmbH" file="CustomRepoMod.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using SmartAssembly.Attributes;
using SN.withSIX.Core.Extensions;

namespace SN.withSIX.Play.Core.Games.Legacy.Mods
{
    [DoNotObfuscate]
    public class CustomRepoMod : Mod
    {
        public CustomRepoMod(Guid id) : base(id) {}
        public override bool IsCustomContent => true;

        public override string GetRemotePath() => Name;

        protected override string GetSlugType() => typeof(Mod).Name.ToUnderscore() + "s";
    }
}
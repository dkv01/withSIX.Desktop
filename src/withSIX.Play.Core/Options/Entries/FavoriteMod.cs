// <copyright company="SIX Networks GmbH" file="FavoriteMod.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using withSIX.Play.Core.Games.Legacy.Mods;

namespace withSIX.Play.Core.Options.Entries
{
    [DataContract(Name = "FavoriteMod",
        Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core.Models")]
    public class FavoriteMod
    {
        [DataMember] readonly string _name;

        public FavoriteMod(IMod mod) {
            _name = mod.Name;
        }

        public IMod Mod { get; private set; }

        public bool Matches(IMod mod) => mod != null && mod.Name == _name;
    }
}
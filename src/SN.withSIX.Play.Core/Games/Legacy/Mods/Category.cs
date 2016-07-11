// <copyright company="SIX Networks GmbH" file="Category.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;


namespace SN.withSIX.Play.Core.Games.Legacy.Mods
{
    
    public class Category : Content
    {
        public Category(Guid id) : base(id) {}
        public override bool HasNotes => false;
        public override string Notes { get; set; }
        public override bool IsFavorite { get; set; }

        public override string ToString() => Name ?? String.Empty;
    }
}
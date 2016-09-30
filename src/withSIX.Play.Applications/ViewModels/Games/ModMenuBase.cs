// <copyright company="SIX Networks GmbH" file="ModMenuBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using withSIX.Play.Applications.ViewModels.Games.Library;
using withSIX.Play.Core.Games.Legacy;

namespace withSIX.Play.Applications.ViewModels.Games
{
    public abstract class ModMenuBase<TContent> : ContextMenuBase<TContent> where TContent : class, IContent
    {
        public ModLibraryViewModel Library { get; }

        protected ModMenuBase(ModLibraryViewModel library) {
            Contract.Requires<ArgumentNullException>(library != null);
            Library = library;
        }
    }
}
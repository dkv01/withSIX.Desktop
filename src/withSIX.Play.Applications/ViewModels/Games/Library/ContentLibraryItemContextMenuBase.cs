﻿// <copyright company="SIX Networks GmbH" file="ContentLibraryItemContextMenuBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using SN.withSIX.Core.Applications.MVVM.ViewModels;
using SN.withSIX.Play.Core.Games.Legacy;
using SN.withSIX.Play.Core.Glue.Helpers;

namespace SN.withSIX.Play.Applications.ViewModels.Games.Library
{
    public abstract class ContentLibraryItemMenuBase<T, T2> : ContextMenuBase<ContentLibraryItemViewModel<T>>
        where T : class, ISelectionList<IContent> where T2 : class
    {
        public T2 Library { get; }

        protected ContentLibraryItemMenuBase(T2 library) {
            Contract.Requires<ArgumentNullException>(library != null);

            Library = library;
        }
    }
}
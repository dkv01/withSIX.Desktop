﻿// <copyright company="SIX Networks GmbH" file="TaskExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Api.Models;
using withSIX.Api.Models.Collections;
using withSIX.Core;
using withSIX.Play.Core.Games.Entities;

namespace withSIX.Play.Core.Extensions
{
    public static class TaskExtensions
    {
        public static Uri ProfileUrl(this CollectionModel collection, Game game) => Tools.Transfer.JoinUri(game.GetUri(), "collections", new ShortGuid(collection.Id),
    collection.Slug);
    }
}
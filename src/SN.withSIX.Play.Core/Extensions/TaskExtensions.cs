// <copyright company="SIX Networks GmbH" file="TaskExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Api.Models;
using withSIX.Api.Models.Collections;
using SN.withSIX.Core;
using SN.withSIX.Play.Core.Games.Entities;

namespace SN.withSIX.Play.Core.Extensions
{
    public static class TaskExtensions
    {
        public static Uri ProfileUrl(this CollectionModel collection, Game game) => Tools.Transfer.JoinUri(game.GetUri(), "collections", new ShortGuid(collection.Id),
    collection.Slug);
    }
}
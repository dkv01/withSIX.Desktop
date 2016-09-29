// <copyright company="SIX Networks GmbH" file="ContentExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using withSIX.Api.Models.Extensions;
using withSIX.Mini.Applications.Models;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Applications.Extensions
{
    public static class ContentExtensions
    {
        public static Dictionary<Guid, ContentStatus> GetStates(this IEnumerable<IContent> installedContent)
            => installedContent
                .ToDictionary(x => x.Id, x => x.MapTo<ContentStatus>());

        public static LaunchAction ToLaunchAction(this PlayAction action)
            => action == PlayAction.Launch ? LaunchAction.Launch : LaunchAction.Default;
    }
}
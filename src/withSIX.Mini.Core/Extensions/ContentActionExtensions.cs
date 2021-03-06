// <copyright company="SIX Networks GmbH" file="ContentActionExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Extensions;
using withSIX.Mini.Core.Games;

namespace withSIX.Mini.Core.Extensions
{
    public static class ContentActionExtensions
    {
        // TODO: Remove need to recreate Specs..
        public static IDownloadContentAction<IInstallableContent> ToInstall(
            this IPlayContentAction<IContent> action) => new DownloadContentAction(
            action
                .Content
                .DistinctBy(x => x.Content)
                .Where(x => x.Content is IInstallableContent)
                .Select(x => new InstallContentSpec((IInstallableContent) x.Content, x.Constraint))
                .ToArray(), action.CancelToken) {
            Force = action.Force,
            HideLaunchAction = action.HideLaunchAction
        };

        public static IEnumerable<ILaunchableContent> GetLaunchables(this ILaunchContentAction<IContent> action)
            => action.Content.SelectMany(x => x.Content.GetLaunchables(x.Constraint)).Distinct();
    }

    public static class ContentExtensions
    {
        internal static string GetContentPath(this Content content, string type, string name)
            => type + "/" + content.Id.ToShortId() + "/" + (name ?? "content").Sluggify(true);
    }
}
// <copyright company="SIX Networks GmbH" file="PlayContentAction.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Threading;

namespace SN.withSIX.Mini.Core.Games
{
    public abstract class PlayContentAction<T> : LaunchContentAction<T>, IPlayContentAction<T> where T : IContent
    {
        protected PlayContentAction(IReadOnlyCollection<IContentSpec<T>> content,
            LaunchType launchType = LaunchType.Default,
            CancellationToken cancelToken = new CancellationToken()) : base(content, launchType, cancelToken) {}

        public bool HideLaunchAction { get; set; }
        public bool Force { get; set; }
    }

    public class PlayContentAction : PlayContentAction<Content>
    {
        public PlayContentAction(LaunchType launchType = LaunchType.Default,
            CancellationToken cancelToken = default(CancellationToken), params IContentSpec<Content>[] content)
            : this(content, launchType, cancelToken) {}

        public PlayContentAction(IReadOnlyCollection<IContentSpec<Content>> content,
            LaunchType launchType = LaunchType.Default,
            CancellationToken cancelToken = default(CancellationToken))
            : base(content, launchType, cancelToken) {}

        public override void Use(IContent content) => content.Use(this);
    }

    public class PlayLocalContentAction : PlayContentAction<LocalContent>
    {
        public PlayLocalContentAction(LaunchType launchType = LaunchType.Default,
            CancellationToken cancelToken = default(CancellationToken),
            params IContentSpec<LocalContent>[] content)
            : this(content, launchType, cancelToken) {}

        public PlayLocalContentAction(IReadOnlyCollection<IContentSpec<LocalContent>> content,
            LaunchType launchType = LaunchType.Default,
            CancellationToken cancelToken = default(CancellationToken))
            : base(content, launchType, cancelToken) {}

        public override void Use(IContent content) => content.Use(this);
    }

    public interface IPlayContentAction<out T> : IDownloadContentAction<T>, ILaunchContentAction<T> where T : IContent {}
}
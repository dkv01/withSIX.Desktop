// <copyright company="SIX Networks GmbH" file="LaunchContentAction.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Threading;

namespace SN.withSIX.Mini.Core.Games
{
    public abstract class LaunchContentAction<T> : ContentAction<T>, ILaunchContentAction<T> where T : IContent
    {
        protected LaunchContentAction(IReadOnlyCollection<IContentSpec<T>> content,
            LaunchType launchType = LaunchType.Default,
            CancellationToken cancelToken = new CancellationToken()) : base(content, cancelToken) {
            LaunchType = launchType;
        }

        public string ServerAddress { get; set; }

        public LaunchType LaunchType { get; }
        public LaunchAction Action { get; set; }
    }

    public class LaunchContentAction : LaunchContentAction<Content>
    {
        public LaunchContentAction(IReadOnlyCollection<IContentSpec<Content>> content,
            LaunchType launchType = LaunchType.Default,
            CancellationToken cancelToken = new CancellationToken()) : base(content, launchType, cancelToken) {}

        public LaunchContentAction(LaunchType launchType = LaunchType.Default,
            CancellationToken cancelToken = new CancellationToken(), params IContentSpec<Content>[] content)
            : this(content, launchType, cancelToken) {}

        public override void Use(IContent content) => content.Use(this);
    }

    public interface ILaunchContentAction<out T> : IContentAction<T> where T : IContent
    {
        LaunchType LaunchType { get; }
        LaunchAction Action { get; }
        string ServerAddress { get; }
    }

    public enum LaunchAction
    {
        Default,
        Launch,
        Join,
        LaunchAsServer,
        LaunchAsDedicatedServer
    }

    public static class LaunchActionExtensions
    {
        public static bool IsAsServer(this LaunchAction action)
            => (action == LaunchAction.LaunchAsServer) || (action == LaunchAction.LaunchAsDedicatedServer);
    }
}
// <copyright company="SIX Networks GmbH" file="ContentAction.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;

namespace withSIX.Mini.Core.Games
{
    public abstract class ActionBase<T> : IAction<T>
    {
        protected ActionBase(T content, CancellationToken cancelToken = default(CancellationToken)) {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            Content = content;
            CancelToken = cancelToken;
        }

        public T Content { get; }
        public CancellationToken CancelToken { get; }
    }

    public abstract class ContentAction<T> : ActionBase<IReadOnlyCollection<IContentSpec<T>>>, IContentAction<T>
        where T : IContent
    {
        protected ContentAction(IReadOnlyCollection<IContentSpec<T>> content,
            CancellationToken cancelToken = default(CancellationToken)) : base(content, cancelToken) {
            Contract.Requires<ArgumentOutOfRangeException>(!content.GroupBy(x => x.Content).Any(x => x.Count() > 1),
                "A content action should not contain duplicates");
        }

        public string Name { get; set; }
        public Uri Href { get; set; }

        public abstract void Use(IContent content);
    }

    public abstract class ContentAction : ContentAction<Content>
    {
        protected ContentAction(CancellationToken cancelToken = default(CancellationToken),
            params IContentSpec<Content>[] content)
            : base(content, cancelToken) {}
    }

    public interface IAction<out T>
    {
        T Content { get; }
        CancellationToken CancelToken { get; }
    }

    public interface IContentAction<out T> : IAction<IReadOnlyCollection<IContentSpec<T>>>
        where T : IContent
    {
        string Name { get; set; }
        Uri Href { get; set; }
        void Use(IContent content);
    }
}
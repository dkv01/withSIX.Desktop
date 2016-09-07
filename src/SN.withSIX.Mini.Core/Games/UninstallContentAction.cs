// <copyright company="SIX Networks GmbH" file="UninstallLocalContentAction.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Threading;

namespace SN.withSIX.Mini.Core.Games
{
    public class UninstallContentAction : ContentAction<IUninstallableContent>
    {
        public UninstallContentAction(IReadOnlyCollection<IContentSpec<IUninstallableContent>> content,
            CancellationToken cancelToken = new CancellationToken()) : base(content, cancelToken) {}

        public UninstallContentAction(CancellationToken cancelToken = default(CancellationToken),
            params IContentSpec<IUninstallableContent>[] content)
            : this(content, cancelToken) {}

        public override void Use(IContent content) => content.Use(this);
    }
}
// <copyright company="SIX Networks GmbH" file="UninstallContent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Content.v3;
using withSIX.Api.Models.Exceptions;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;
using withSIX.Mini.Applications.Attributes;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Services.ContentInstaller;
using ContentGuidSpec = withSIX.Mini.Core.Games.ContentGuidSpec;

namespace withSIX.Mini.Applications.Features.Main.Games
{
    public static class ContentExtensions
    {
        public static T FindContentOrThrow<T>(this IEnumerable<T> source, Guid id) where T : IHaveId<Guid> {
            try {
                return source.FindOrThrow(id);
            } catch (NotFoundException ex) {
                throw new RequestedContentNotFoundException($"The desired content with id: {id} was not found", ex);
            }
        }
    }

    [ApiUserAction("Uninstall")]
    public class UninstallContent : SingleCntentBase, ICancellable, INotifyAction, ICancelable
    {
        public UninstallContent(Guid gameId, ContentGuidSpec content) : base(gameId, content) {}

        public CancellationToken CancelToken { get; set; }
        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);

        public UninstallContentAction GetAction(Game game) {
            var content = game.Contents.OfType<IUninstallableContent>().FindContentOrThrow(Content.Id);
            var hasPath = content as IHavePath;
            return new UninstallContentAction(CancelToken, new UninstallContentSpec(content, Content.Constraint)) {
                Name = Name,
                Href =
                    Href ??
                    (hasPath == null ? null : new Uri("http://withsix.com/p/" + game.GetContentPath(hasPath, Name)))
            };
        }
    }

    [ApiUserAction("Uninstall")]
    public class UninstallContents : GameContentBaseWithInfo, ICancellable, INotifyAction, ICancelable
    {
        public UninstallContents(Guid gameId, List<Guid> contents) : base(gameId) {
            Ids = contents;
        }

        public List<Guid> Ids { get; }
        public CancellationToken CancelToken { get; set; }
        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);

        public UninstallContentAction GetAction(Game game) => new UninstallContentAction(
            Ids.Select(
                    x => new UninstallContentSpec(game.Contents.OfType<IUninstallableContent>().FindContentOrThrow(x)))
                .ToArray(), CancelToken) {Name = Name, Href = GetHref(game)};


        public class UninstallInstalledItemHandler : DbCommandBase, IAsyncVoidCommandHandler<UninstallContent>,
            IAsyncVoidCommandHandler<UninstallContents>
        {
            readonly IContentInstallationService _contentInstallation;

            public UninstallInstalledItemHandler(IDbContextLocator dbContextLocator,
                IContentInstallationService contentInstallation) : base(dbContextLocator) {
                _contentInstallation = contentInstallation;
            }

            // TODO: LocalContent doesnt need a spec??
            public async Task<Unit> Handle(UninstallContent request) {
                var game =
                    await
                        GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
                await game.Uninstall(_contentInstallation, request.GetAction(game)).ConfigureAwait(false);
                return Unit.Value;
            }

            // TODO: LocalContent doesnt need a spec??
            public async Task<Unit> Handle(UninstallContents request) {
                var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
                await game.Uninstall(_contentInstallation, request.GetAction(game)).ConfigureAwait(false);
                return Unit.Value;
            }
        }
    }
}
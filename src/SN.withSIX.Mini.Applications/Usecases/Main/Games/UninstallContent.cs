// <copyright company="SIX Networks GmbH" file="UninstallContent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShortBus;
using withSIX.Api.Models.Exceptions;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Applications.Attributes;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace SN.withSIX.Mini.Applications.Usecases.Main.Games
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
    public class UninstallContent : SingleCntentBase, INeedCancellationTokenSource, INotifyAction, ICancelable
    {
        public UninstallContent(Guid gameId, ContentGuidSpec content) : base(gameId, content) {}

        public DoneCancellationTokenSource CTS { get; set; }
        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);

        public UninstallLocalContentAction GetAction(Game game) {
            var content = game.Contents.OfType<IUninstallableContent>().FindContentOrThrow(Content.Id);
            var hasPath = content as IHavePath;
            var href = hasPath == null ? null : new Uri("http://withsix.com/p/" + game.GetContentPath(hasPath));
            return new UninstallLocalContentAction(content: new UninstallContentSpec(content, Content.Constraint)) {
                Name = content.Name,
                Href = href
            };
        }
    }

    [ApiUserAction("Uninstall")]
    public class UninstallContents : GameContentBaseWithInfo, INeedCancellationTokenSource, INotifyAction, ICancelable
    {
        public UninstallContents(Guid gameId, List<Guid> contents) : base(gameId) {
            Ids = contents;
        }

        public List<Guid> Ids { get; }
        public DoneCancellationTokenSource CTS { get; set; }
        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);

        public UninstallLocalContentAction GetAction(Game game) => new UninstallLocalContentAction(
            Ids.Select(
                x => new UninstallContentSpec(game.Contents.OfType<IUninstallableContent>().FindContentOrThrow(x)))
                .ToArray(), CTS.Token) {Name = Name, Href = GetHref(game)};


        public class UninstallInstalledItemHandler : DbCommandBase, IAsyncVoidCommandHandler<UninstallContent>,
            IAsyncVoidCommandHandler<UninstallContents>
        {
            readonly IContentInstallationService _contentInstallation;

            public UninstallInstalledItemHandler(IDbContextLocator dbContextLocator,
                IContentInstallationService contentInstallation) : base(dbContextLocator) {
                _contentInstallation = contentInstallation;
            }

            // TODO: LocalContent doesnt need a spec??
            public async Task<UnitType> HandleAsync(UninstallContent request) {
                var game =
                    await
                        GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
                await game.Uninstall(_contentInstallation, request.GetAction(game)).ConfigureAwait(false);
                return UnitType.Default;
            }

            // TODO: LocalContent doesnt need a spec??
            public async Task<UnitType> HandleAsync(UninstallContents request) {
                var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
                await game.Uninstall(_contentInstallation, request.GetAction(game)).ConfigureAwait(false);
                return UnitType.Default;
            }
        }
    }
}
// <copyright company="SIX Networks GmbH" file="InstallContent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShortBus;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Attributes;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Applications.Usecases.Main.Games;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    [ApiUserAction("Install")]
    public class InstallContent : SingleCntentBase, INeedCancellationTokenSource,
        IOverrideNotificationTitle, INotifyAction, IHaveNexAction, IUseContent, ICancelable
    {
        public InstallContent(Guid gameId, ContentGuidSpec content) : base(gameId, content) {}
        public bool HideLaunchAction { get; set; }
        public bool Force { get; set; }

        public IAsyncVoidCommandBase GetNextAction()
            => new LaunchContent(GameId, Content);

        public DoneCancellationTokenSource CTS { get; set; }

        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);
        public string ActionTitleOverride => Force ? "Diagnose" : null;
        public string PauseTitleOverride => Force ? "Cancel" : null;

        public DownloadContentAction GetAction(Game game) {
            var content = game.Contents.FindContentOrThrow(Content.Id);
            var hasPath = content as IHavePath;
            var href = hasPath == null ? null : new Uri("http://withsix.com/p/" + game.GetContentPath(hasPath));
            return new DownloadContentAction(CTS.Token,
                new InstallContentSpec((IInstallableContent) content, Content.Constraint)) {
                    HideLaunchAction = HideLaunchAction,
                    Force = Force,
                    Name = content.Name,
                    Href = href
                };
        }
    }

    [ApiUserAction("Install")]
    public class InstallContents : ContentsBase, INeedCancellationTokenSource,
        IOverrideNotificationTitle, INotifyAction, IHaveNexAction, IUseContent, ICancelable
    {
        public InstallContents(Guid gameId, List<ContentGuidSpec> contents) : base(gameId, contents) {}

        public bool HideLaunchAction { get; set; }
        public bool Force { get; set; }

        public IAsyncVoidCommandBase GetNextAction()
            => new LaunchContents(GameId, Contents) {Name = Name};

        public DoneCancellationTokenSource CTS { get; set; }
        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);
        public string ActionTitleOverride => Force ? "Diagnose" : null;
        public string PauseTitleOverride => Force ? "Cancel" : null;

        public DownloadContentAction GetAction(Game game) => new DownloadContentAction(CTS.Token,
            Contents.Select(x => new {Content = game.Contents.FindContentOrThrow(x.Id), x.Constraint})
                .Where(x => x.Content is IInstallableContent)
                .Select(
                    x => new InstallContentSpec((IInstallableContent) x.Content, x.Constraint))
                .ToArray()) {
                    Name = Name,
                    HideLaunchAction = HideLaunchAction,
                    Force = Force,
                    Href = GetHref(game)
                };
    }

    public class InstallCollection : InstallContent, INeedSynchronization
    {
        public InstallCollection(Guid gameId, ContentGuidSpec content) : base(gameId, content) {}

        public Task Synchronize(Game game, INetworkContentSyncer syncer)
            => SyncCollectionsHandler.DealWithCollections(game, new[] {Content}, syncer);
    }

    public interface INeedSynchronization
    {
        Task Synchronize(Game game, INetworkContentSyncer syncer);
    }

    public class InstallContentHandler : ApiDbCommandBase, IAsyncVoidCommandHandler<InstallContent>,
        IAsyncVoidCommandHandler<InstallCollection>, IAsyncVoidCommandHandler<InstallContents>
    {
        readonly IContentInstallationService _contentInstallation;

        public InstallContentHandler(IDbContextLocator dbContextLocator,
            IContentInstallationService contentInstallation)
            : base(dbContextLocator) {
            _contentInstallation = contentInstallation;
        }

        public async Task<UnitType> HandleAsync(InstallCollection request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            await InstallContent(request, game).ConfigureAwait(false);
            return UnitType.Default;
        }

        public async Task<UnitType> HandleAsync(InstallContent request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            await InstallContent(request, game).ConfigureAwait(false);
            return UnitType.Default;
        }

        public async Task<UnitType> HandleAsync(InstallContents request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            await game.Install(_contentInstallation, request.GetAction(game)).ConfigureAwait(false);
            return UnitType.Default;
        }

        private Task InstallContent(InstallContent request, Game game)
            => game.Install(_contentInstallation, request.GetAction(game));
    }
}
﻿// <copyright company="SIX Networks GmbH" file="InstallContent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Extensions;
using withSIX.Mini.Applications.Attributes;
using withSIX.Mini.Applications.Features.Main.Games;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Services.ContentInstaller;

namespace withSIX.Mini.Applications.Features.Main
{
    [ApiUserAction("Install")]
    public class InstallContent : SingleCntentBase, ICancellable,
        IOverrideNotificationTitle, INotifyAction, IHaveNexAction, IUseContent, ICancelable
    {
        public InstallContent(Guid gameId, ContentGuidSpec content) : base(gameId, content) {}
        public bool HideLaunchAction { get; set; }
        public bool Force { get; set; }

        public CancellationToken CancelToken { get; set; }

        public ICommandBase GetNextAction()
            => new LaunchContent(GameId, Content) {Name = Name};

        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);
        public string ActionTitleOverride => Force ? "Diagnose" : null;
        public string PauseTitleOverride => Force ? "Cancel" : null;

        public DownloadContentAction GetAction(Game game) {
            var content = game.Contents.OfType<IInstallableContent>().FindContentOrThrow(Content.Id);
            var hasPath = content as IHavePath;
            return new DownloadContentAction(CancelToken,
                new InstallContentSpec(content, Content.Constraint)) {
                HideLaunchAction = HideLaunchAction,
                Force = Force,
                Name = Name,
                Href =
                    Href ??
                    (hasPath == null ? null : new Uri("http://withsix.com/p/" + game.GetContentPath(hasPath, Name)))
            };
        }
    }

    [ApiUserAction("Install")]
    public class InstallContents : ContentsBase, ICancellable,
        IOverrideNotificationTitle, INotifyAction, IHaveNexAction, IUseContent, ICancelable
    {
        public InstallContents(Guid gameId, List<ContentGuidSpec> contents) : base(gameId, contents) {}

        public bool HideLaunchAction { get; set; }
        public bool Force { get; set; }

        public CancellationToken CancelToken { get; set; }

        public ICommandBase GetNextAction()
            => new LaunchContents(GameId, Contents) {Name = Name};

        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);
        public string ActionTitleOverride => Force ? "Diagnose" : null;
        public string PauseTitleOverride => Force ? "Cancel" : null;

        public DownloadContentAction GetAction(Game game) => new DownloadContentAction(CancelToken,
            Contents
                // Specifying multiple times the same content is invalid - we should block this already on input?
                .DistinctBy(x => x.Id)
                .Select(x => new {Content = game.Contents.FindContentOrThrow(x.Id) as IInstallableContent, x.Constraint})
                .Where(x => x.Content != null)
                .Select(x => new InstallContentSpec(x.Content, x.Constraint))
                .ToArray()) {
            Name = Name,
            HideLaunchAction = HideLaunchAction,
            Force = Force,
            Href = GetHref(game)
        };
    }

    [ApiUserAction("Install")]
    public class InstallSteamContents : ContentsIntBase, ICancellable,
        IOverrideNotificationTitle, INotifyAction, IHaveNexAction, IUseContent, ICancelable
    {
        public InstallSteamContents(Guid gameId, List<ContentIntSpec> contents) : base(gameId, contents) {}

        public bool HideLaunchAction { get; set; }
        public bool Force { get; set; }

        public CancellationToken CancelToken { get; set; }

        public ICommandBase GetNextAction()
            =>
                new LaunchContents(GameId,
                    Contents.Select(
                        x =>
                            new ContentGuidSpec(
                                x.Id.CreateSteamContentIdGuid(),
                                x.Constraint)).ToList()) {
                    Name = Name
                };

        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);
        public string ActionTitleOverride => Force ? "Diagnose" : null;
        public string PauseTitleOverride => Force ? "Cancel" : null;

        public DownloadContentAction GetAction(Game game) => new DownloadContentAction(CancelToken,
            Contents
                .Select(x => new {Content = GetOrCreateContent(game, x) as IInstallableContent, x.Constraint})
                .Select(x => new {x.Content, x.Constraint})
                .Where(x => x.Content != null)
                .DistinctBy(x => x.Content)
                .Select(x => new InstallContentSpec(x.Content, x.Constraint))
                .ToArray()) {
            Name = Name,
            HideLaunchAction = HideLaunchAction,
            Force = Force,
            Href = GetHref(game)
        };

        private static Content GetOrCreateContent(Game game, ContentIntSpec x) {
            var guid = x.Id.CreateSteamContentIdGuid();
            var content = game.Contents.Find(guid);
            if (content != null)
                return content;
            content = ModNetworkContent.FromSteamId(x.Id, game.Id);
            game.Contents.Add(content);
            return content;
        }
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

    public class InstallContentHandler : ApiDbCommandBase, IAsyncRequestHandler<InstallContent>,
        IAsyncRequestHandler<InstallCollection>, IAsyncRequestHandler<InstallContents>,
        IAsyncRequestHandler<InstallSteamContents>
    {
        readonly IContentInstallationService _contentInstallation;

        public InstallContentHandler(IDbContextLocator dbContextLocator,
            IContentInstallationService contentInstallation)
            : base(dbContextLocator) {
            _contentInstallation = contentInstallation;
        }

        public async Task Handle(InstallCollection request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            await InstallContent(request, game).ConfigureAwait(false);
        }

        public async Task Handle(InstallContent request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            await InstallContent(request, game).ConfigureAwait(false);
        }

        public async Task Handle(InstallContents request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            await game.Install(_contentInstallation, request.GetAction(game)).ConfigureAwait(false);
        }

        public async Task Handle(InstallSteamContents request) {
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            await game.Install(_contentInstallation, request.GetAction(game)).ConfigureAwait(false);
        }

        private Task InstallContent(InstallContent request, Game game)
            => game.Install(_contentInstallation, request.GetAction(game));
    }
}
// <copyright company="SIX Networks GmbH" file="AddExternalMod.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NDepend.Path;
using SN.withSIX.Core.Applications.Services;
using SN.withSIX.Mini.Applications.Attributes;
using SN.withSIX.Mini.Applications.Services;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using withSIX.Api.Models.Content;
using withSIX.Api.Models.Exceptions;
using withSIX.Api.Models.Games;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    [ApiUserAction("Install")]
    public class AddExternalMod : IAsyncVoidCommand, IUseContent, INotifyAction, IHaveNexAction
    {
        readonly Regex nexus = new Regex(@"http://www.nexusmods.com/([^\/#])/mods/([^\/#])/");

        readonly Regex nmsm = new Regex(@"http://nomansskymods.com/mods/([^\/#])");

        public AddExternalMod(string fileName, Uri referrer) {
            FileName = fileName;
            Referrer = referrer;

            var r = Referrer.ToString();
            var m = nmsm.Match(r);
            if (m.Success) {
                GameId = GameGuids.NMS;
                Publisher = Publisher.NoMansSkyMods;
                PubId = nmsm.Match(r).Groups[1].Value;
                // TODO: Install as local mod when unknown etc
            } else {
                m = nexus.Match(r);
                if (m.Success) {
                    var g = m.Groups[1].Value;
                    if (g != "fallout4") {
                        Error = new ValidationException("The game is currently not supported: " + g);
                    }
                    GameId = GameGuids.Fallout4;
                    Publisher = Publisher.NexusMods;
                    PubId = nmsm.Match(r).Groups[2].Value;
                } else
                    Error = new ValidationException("The link is not recognized");
            }
        }

        public Exception Error { get; set; }

        IContentAction<IContent> IHandleAction.GetAction(Game game) => GetAction(game);

        public string PubId { get; set; }

        public Publisher Publisher { get; }

        public Uri Referrer { get; }
        public string FileName { get; }

        public CancellationToken CancelToken { get; set; }

        public bool Force { get; set; }

        public bool HideLaunchAction { get; set; }

        public Guid ClientId { get; set; }
        public Guid RequestId { get; }
        public Guid GameId { get; }

        public ContentGuidSpec Content { get; set; }

        public DownloadContentAction GetAction(Game game) {
            var c =
                game.NetworkContent.FirstOrDefault(
                    x => x.Publishers.Any(p => p.Publisher == Publisher && p.PublisherId == PubId));
            if (c == null)
                throw new NotFoundException("Content not found");
            Content = new ContentGuidSpec(c.Id, null);
            var hasPath = c as IHavePath;
            var href = hasPath == null ? null : new Uri("http://withsix.com/p/" + game.GetContentPath(hasPath));
            return new DownloadContentAction(CancelToken,
                new InstallContentSpec(c, Content.Constraint)) {
                    HideLaunchAction = HideLaunchAction,
                    Force = Force,
                    Name = c.Name,
                    Href = href
                };
        }


        public string ActionTitleOverride => Force ? "Diagnose" : null;
        public string PauseTitleOverride => Force ? "Cancel" : null;

        public IAsyncVoidCommandBase GetNextAction()
            => new LaunchContent(GameId, Content);
    }

    public class AddExternalModHandler : ApiDbCommandBase, IAsyncVoidCommandHandler<AddExternalMod>
    {
        readonly IContentInstallationService _contentInstallation;
        private readonly IExternalFileDownloader _fd;

        public AddExternalModHandler(IDbContextLocator dbContextLocator,
            IContentInstallationService contentInstallation, IExternalFileDownloader fd)
            : base(dbContextLocator) {
            _contentInstallation = contentInstallation;
            _fd = fd;
        }

        public async Task<Unit> Handle(AddExternalMod request) {
            // TODO
            if (request.Error != null)
                throw request.Error;
            var game = await GameContext.FindGameOrThrowAsync(request).ConfigureAwait(false);
            var action = request.GetAction(game);
            _fd.RegisterExisting(
                game.GetPublisherUrl(action.Content.Select(x => x.Content).OfType<NetworkContent>().First().Source),
                request.FileName.ToAbsoluteFilePath());
            await game.Install(_contentInstallation, action).ConfigureAwait(false);
            return Unit.Value;
        }
    }
}
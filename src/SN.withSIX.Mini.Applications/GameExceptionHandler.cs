// <copyright company="SIX Networks GmbH" file="GameExceptionHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using ReactiveUI;
using SN.withSIX.Api.Models.Exceptions;
using SN.withSIX.Core;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Mini.Core.Games;
using SN.withSIX.Mini.Core.Games.Services.ContentInstaller;
using SN.withSIX.Sync.Core.Packages;
using SN.withSIX.Sync.Core.Repositories;
using SN.withSIX.Sync.Core.Transfer.MirrorSelectors;

namespace SN.withSIX.Mini.Applications
{
    public class GameExceptionHandler : BasicExternalExceptionhandler
    {
        private const string CurrentlyNotAvailable =
            "The content you are downloading is currently not available. The files might be still syncing, or are removed from our network. Or there is a network error. Please try again later.";

        private const string CouldNotFindTheDesiredContent =
            "This might indicate a synchronization failure, the content might've been deleted, or you might not have access to the content";

        public override UserError HandleException(Exception ex, string action = "Action") {
            Contract.Requires<ArgumentNullException>(action != null);
            return Handle((dynamic) ex, action);
        }

        protected static RecoverableUserError Handle(GameIsRunningException ex, string action)
            => new RecoverableUserError(ex, ex.Message, "Please close the game and try again");

        // TODO: Better handler where we guide the user to go to the settings, and configure the game, then retry?
        protected static InformationalUserError Handle(GameNotInstalledException ex, string action)
            => new ConfigureGameFirstUserError(ex, ex.Message, "Please configure the game first in the Settings");

        protected static RecoverableUserError Handle(AlreadyLockedException ex, string action)
            => new RecoverableUserError(ex, "Currently only one action per game is supported. Wait until the action is finished and try again", "Unsupported");

        protected static RecoverableUserError Handle(QueueProcessingError ex, string action)
            => new RecoverableUserError(ex,
                "The following errors occurred: " +
                string.Join("\n", ex.Flatten().InnerExceptions.Select(x => x.Message)) + "\n\nSee log for more details",
                "One or more errors occured while processing");

        protected static InformationalUserError Handle(NoSourceFoundException ex, string action)
            =>
                new InformationalUserError(ex,
                    CurrentlyNotAvailable,
                    "Could not find the desired content");

        protected static InformationalUserError Handle(RequestedResourceNotFoundException ex, string action)
            => new InformationalUserError(ex,
                CouldNotFindTheDesiredContent,
                "Could not find the desired content");

        protected static InformationalUserError Handle(NotFoundException ex, string action)
            => new InformationalUserError(ex, CouldNotFindTheDesiredContent, "Could not find the desired content");

        
        protected static InformationalUserError Handle(SynqPathException ex, string action)
            => new InformationalUserError(ex, ex.Message, "Please reconfigure the Sync directory");

        protected static RecoverableUserError Handle(HostListExhausted ex, string action)
            => new RecoverableUserError(ex, @"There was an issue downloading the content.
Network or connection issues might prevent the download to succeed.

Please confirm your internet connection, and try again", "Download error");
    }

    public class ConfigureGameFirstUserError : InformationalUserError
    {
        public ConfigureGameFirstUserError(Exception exception, string message, string title = null)
            : base(exception, message, title) {}
    }
}
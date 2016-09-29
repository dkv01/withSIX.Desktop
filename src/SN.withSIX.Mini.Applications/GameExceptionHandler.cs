// <copyright company="SIX Networks GmbH" file="GameExceptionHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using withSIX.Api.Models.Exceptions;
using withSIX.Core;
using withSIX.Core.Applications.Errors;
using withSIX.Mini.Applications.Services;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Mini.Core.Games;
using withSIX.Mini.Core.Games.Services.ContentInstaller;
using withSIX.Steam.Core;
using withSIX.Sync.Core.Packages;
using withSIX.Sync.Core.Repositories;
using withSIX.Sync.Core.Transfer.MirrorSelectors;

namespace withSIX.Mini.Applications
{
    public class GameExceptionHandler : BasicExternalExceptionhandler
    {
        private const string CurrentlyNotAvailable =
            "The content you are downloading is currently not available. The files might be still syncing, or are removed from our network. Or there is a network error. Please try again later.";

        private const string CouldNotFindTheDesiredContent =
            "This might indicate a synchronization failure, the content might've been deleted, or you might not have access to the content";

        public override UserErrorModel HandleException(Exception ex, string action = "Action") {
            Contract.Requires<ArgumentNullException>(action != null);
            return Handle((dynamic) ex, action);
        }

        protected static RecoverableUserError Handle(SteamNotFoundException ex, string action)
            => new RecoverableUserError(ex, "Requires the Steam Client", ex.Message);

        protected static RecoverableUserError Handle(SteamInitializationException ex, string action)
            => new RecoverableUserError(ex, "Running Steam Client required", ex.Message);

        protected static RecoverableUserError Handle(NotDetectedAsSteamGame ex, string action)
            => new RecoverableUserError(ex, "Requires a Steam version of the game", ex.Message);

        protected static RecoverableUserError Handle(SetupException ex, string action) =>
            new RecoverableUserError(ex, "Configuration error", ex.Message);

        protected static RecoverableUserError Handle(GameIsRunningException ex, string action)
            => new RecoverableUserError(ex, "Please close the game and try again", ex.Message);

        // TODO: Better handler where we guide the user to go to the settings, and configure the game, then retry?
        protected static RecoverableUserError Handle(GameNotInstalledException ex, string action)
            =>
            new ConfigureGameFirstUserError(ex, ex.Message,
                "Please configure the game first in the Settings, then retry");

        protected static RecoverableUserError Handle(AlreadyLockedException ex, string action)
            =>
            new RecoverableUserError(ex, "Unsupported",
                "Currently only one action per game is supported. Wait until the action is finished and try again");

        protected static RecoverableUserError Handle(QueueProcessingError ex, string action)
            => new RecoverableUserError(ex,
                "One or more errors occured while processing",
                "The following errors occurred: " +
                string.Join("\n", ex.Flatten().InnerExceptions.Select(x => x.Message)) + "\n\nSee log for more details");

        protected static InformationalUserError Handle(NoSourceFoundException ex, string action)
            =>
            new InformationalUserError(ex,
                "Could not find the desired content",
                CurrentlyNotAvailable);

        protected static InformationalUserError Handle(RequestedResourceNotFoundException ex, string action)
            => new InformationalUserError(ex,
                "Could not find the desired content",
                CouldNotFindTheDesiredContent);

        protected static InformationalUserError Handle(NotFoundException ex, string action)
            => new InformationalUserError(ex, "Could not find the desired content", CouldNotFindTheDesiredContent);


        protected static InformationalUserError Handle(SynqPathException ex, string action)
            => new InformationalUserError(ex, "Please reconfigure the Sync directory", ex.Message);

        protected static RecoverableUserError Handle(HostListExhausted ex, string action)
            => new RecoverableUserError(ex, "Download error",
                @"There was an issue downloading the content.
Network or connection issues might prevent the download to succeed.

Please confirm your internet connection, and try again");
    }

    public class ConfigureGameFirstUserError : RecoverableUserError
    {
        public ConfigureGameFirstUserError(Exception exception, string message, string title = null)
            : base(exception, title, message) {}
    }
}
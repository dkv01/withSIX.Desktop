// <copyright company="SIX Networks GmbH" file="SteamDepotDownloader.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using ReactiveUI;
using SN.withSIX.Core.Applications.Errors;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Core.Logging;
using SN.withSIX.Mini.Applications.Models;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Steam.Core.SteamKit.DepotDownloader;
using SN.withSIX.Sync.Core.Transfer;
using withSIX.Api.Models.Exceptions;

namespace SN.withSIX.Mini.Applications.Services
{
    public class SteamDepotDownloader
    {
        private readonly IDbContextLocator _contextLocator;
        private readonly ISteamDownloader _steamDownloader;

        public SteamDepotDownloader(ISteamDownloader steamDownloader, IDbContextLocator contextLocator) {
            _steamDownloader = steamDownloader;
            _contextLocator = contextLocator;
        }

        public async Task PerformSteamDepotDownloaderAction(ITProgress progress, ulong publisherId,
            IAbsoluteDirectoryPath destination, CancellationToken cancelToken) {
            var settings = await _contextLocator.GetReadOnlySettingsContext().GetSettings().ConfigureAwait(false);
            var creds = settings.Secure.SteamCredentials;
            if (creds == null)
                settings.Secure.SteamCredentials = creds = await RequestSteamCredentials(creds).ConfigureAwait(false);

            ContentDownloader.GetDetails = msg => RequestSteamGuardCode();
            ContentDownloader.Log = msg => {
                MainLog.Logger.Info(msg);
                Console.WriteLine(msg);
            };

            try1:
            try {
                await
                    _steamDownloader.Download(publisherId, destination,
                        new LoginDetails(creds.Username, creds.Password), progress.Update, cancelToken)
                        .ConfigureAwait(false);
            } catch (UnauthorizedException ex) {
                MainLog.Logger.FormattedWarnException(ex, "Steam download failed auth");
                settings.Secure.SteamCredentials = creds = await RequestSteamCredentials(creds).ConfigureAwait(false);
                goto try1;
            }
        }

        private static async Task<Credentials> RequestSteamCredentials(IAuthInfo creds) {
            var uer = new UsernamePasswordUserError(new Exception("Missing Steam credentials"),
                "Steam credentials required", "Please provide the login credentials to download the content from Steam",
                new Dictionary<string, object> {
                    {"userName", creds?.Username ?? ""},
                    {"password", ""} // , currentAuthInfo.Password ?? "" .. lets think about this ;-)
                });
            if (await
                UserError.Throw(uer) !=
                RecoveryOptionResult.RetryOperation)
                throw new OperationCanceledException("Steam credentials not provided");
            return new Credentials((uer.ContextInfo["userName"] as string).Trim(),
                (uer.ContextInfo["password"] as string).Trim());
        }

        private static async Task<string> RequestSteamGuardCode() {
            var uer = new InputUserError(new Exception("Require SteamGuard code"),
                "SteamGuard code required",
                "Please provide the guard code to download the content from Steam (from your Authenticator App or sent to your email)",
                new Dictionary<string, object> {
                    {"input", ""}
                });
            if (await
                UserError.Throw(uer) !=
                RecoveryOptionResult.RetryOperation)
                throw new OperationCanceledException("SteamGuard code not provided");
            return (uer.ContextInfo["input"] as string).Trim();
        }
    }
}
// <copyright company="SIX Networks GmbH" file="AuthDownloadWrapper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Errors;
using SN.withSIX.Sync.Core.Transfer;

namespace SN.withSIX.Mini.Applications.Services
{
    public class AuthDownloadWrapper
    {
        private readonly IAuthProvider _authProvider;

        public AuthDownloadWrapper(IAuthProvider authProvider) {
            _authProvider = authProvider;
        }

        public async Task WrapAction(Func<Uri, Task> action, Uri uri) {
            Contract.Requires<ArgumentNullException>(action != null);
            Contract.Requires<ArgumentNullException>(uri != null);
            retry:
            try {
                await action(uri).ConfigureAwait(false);
            } catch (HttpDownloadException e) {
                // TODO: Or should we rather abstract this away into the downloader exceptions instead?
                if (e.StatusCode != HttpStatusCode.Unauthorized)
                    throw;
                var r = await HandleError(uri).ConfigureAwait(false);
                if (r == null)
                    throw new OperationCanceledException("The user did not enter the requested info", e);
                uri = r;
                goto retry;
            } catch (FtpDownloadException e) {
                if (e.StatusCode != FtpStatusCode.NotLoggedIn && e.StatusCode != FtpStatusCode.AccountNeeded &&
                    e.StatusCode != FtpStatusCode.NeedLoginAccount)
                    throw;
                var r = await HandleError(uri).ConfigureAwait(false);
                if (r == null)
                    throw new OperationCanceledException("The user did not enter the requested info", e);
                uri = r;
                goto retry;
            }
        }

        async Task<Uri> HandleError(Uri uri) {
            var currentAuthInfo = _authProvider.GetAuthInfoFromUriWithCache(uri);
            var userError = new UsernamePasswordUserError("Username password required", "Please enter the info",
                RecoveryCommandsImmediate.RetryCommands, new Dictionary<string, object> {
                    {"userName", currentAuthInfo.Username ?? ""},
                    {"password", ""} // , currentAuthInfo.Password ?? "" .. lets think about this ;-)
                });
            var r = await UserError.Throw(userError);
            if (r != RecoveryOptionResult.RetryOperation)
                return null;
            var userName = userError.ContextInfo["userName"] as string;
            var password = userError.ContextInfo["password"] as string;
            _authProvider.SetNonPersistentAuthInfo(uri, new AuthInfo(userName, password));
            return _authProvider.HandleUriAuth(uri, userName, password);
        }
    }
}
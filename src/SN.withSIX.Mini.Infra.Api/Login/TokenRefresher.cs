// <copyright company="SIX Networks GmbH" file="TokenRefresher.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ShortBus;
using SN.withSIX.Api.Models.Exceptions;
using SN.withSIX.Api.Models.Premium;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Infrastructure;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Core.Logging;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Models;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Sync.Core.Transfer;
using Synercoding.Encryption.Hashing;
using Synercoding.Encryption.Symmetrical;

namespace SN.withSIX.Mini.Infra.Api.Login
{
    // TODO: Don't work with storage directly?
    public class TokenRefresher : IInfrastructureService, ITokenRefresher
    {
        readonly IOauthConnect _connect;
        readonly PremiumHandler _premiumRefresher;

        public TokenRefresher(IOauthConnect connect) {
            _connect = connect;
            _premiumRefresher = new PremiumHandler();
        }

        public async Task HandleLogin(AccessInfo info, Settings settings) {
            Contract.Requires<ArgumentNullException>(info != null);
            if (info.AccessToken != null)
                await TryHandleLoggedIn(info, settings).ConfigureAwait(false);
            else
                await HandleLoggedOut(settings).ConfigureAwait(false);
        }

        private async Task TryHandleLoggedIn(AccessInfo accessInfo, Settings settings) {
            try {
                await HandleLoggedIn(accessInfo, settings).ConfigureAwait(false);
                // try fetch userinfo. if failed, consider logged out, perhaps ask the website for login again
            } catch (Exception ex) {
                MainLog.Logger.FormattedWarnException(ex, "Failure while processing login info");
                await HandleLoggedOut(settings).ConfigureAwait(false);
            }
        }

        private async Task HandleLoggedIn(AccessInfo accessInfo, Settings settings) {
            var userInfo =
                await
                    _connect.GetUserInfo(CommonUrls.AuthorizationEndpoints.UserInfoEndpoint,
                        accessInfo.AccessToken)
                        .ConfigureAwait(false);
            var login = new LoggedInInfo(BuildAccountInfo(userInfo),
                new AuthenticationInfo {AccessToken = accessInfo.AccessToken});
            settings.UpdateLogin(login);
            if (login.Account.Roles.Contains("premium")) {
                await
                    _premiumRefresher.ProcessPremium(GetPremiumTokenClaim(userInfo), settings)
                        .ConfigureAwait(false);
            }
        }

        private static string GetPremiumTokenClaim(IUserInfoResponse userInfo)
            => GetClaim(userInfo, CustomClaimTypes.PremiumToken);

        private Task HandleLoggedOut(Settings settings) {
            settings.UpdateLogin(LoginInfo.Default);
            return _premiumRefresher.Logout(settings);
        }

        static AccountInfo BuildAccountInfo(IUserInfoResponse userInfo) {
            var avatarUrl = GetAvatarUrlClaim(userInfo);
            var updatedAt = GetAvatarUpdatedAtClaim(userInfo);
            return new AccountInfo {
                Id = Guid.Parse(GetIdClaim(userInfo)),
                Roles = GetRoleClaims(userInfo),
                DisplayName = GetNicknameClaim(userInfo),
                UserName = GetUsernameClaim(userInfo),
                AvatarURL = avatarUrl,
                HasAvatar = GetHasAvatarClaim(userInfo) == "true",
                AvatarUpdatedAt = updatedAt == null ? 0 : long.Parse(updatedAt),
                EmailMd5 = GetEmailMd5Claim(userInfo)
            };
        }

        private static string GetEmailMd5Claim(IUserInfoResponse userInfo)
            => GetClaim(userInfo, CustomClaimTypes.EmailMd5);

        private static string GetHasAvatarClaim(IUserInfoResponse userInfo)
            => GetClaim(userInfo, CustomClaimTypes.HasAvatar);

        private static string GetUsernameClaim(IUserInfoResponse userInfo) => GetClaim(userInfo, "preferred_username");

        private static string GetNicknameClaim(IUserInfoResponse userInfo) => GetClaim(userInfo, "nickname");

        private static List<string> GetRoleClaims(IUserInfoResponse userInfo)
            => userInfo.Claims.Where(x => x.Item1 == "role").Select(x => x.Item2).ToList();

        private static string GetIdClaim(IUserInfoResponse userInfo) => GetClaim(userInfo, "sub");

        private static string GetAvatarUpdatedAtClaim(IUserInfoResponse userInfo)
            => GetClaim(userInfo, CustomClaimTypes.AvatarUpdatedAt);

        private static string GetAvatarUrlClaim(IUserInfoResponse userInfo)
            => GetClaim(userInfo, CustomClaimTypes.AvatarUrl);

        static string GetClaim(IUserInfoResponse userInfo, string claimType) {
            var claim = userInfo.Claims.FirstOrDefault(x => x.Item1 == claimType);
            return claim?.Item2;
        }

        class PremiumHandler
        {
            bool _firstCompleted;

            public async Task ProcessPremium(string encryptedPremiumToken, Settings settings) {
                // TODO
                //var apiKey = _connectionManager.ApiKey;
                //var apiKey = DomainEvilGlobal.Settings.AppOptions.Id.ToString().Sha256String();
                var apiKey = settings.Secure.Login.Account.Id.ToString();
                var premiumToken =
                    await GetPremiumTokenInternal(encryptedPremiumToken, apiKey, settings).ConfigureAwait(false);
                await UpdateToken(premiumToken, settings).ConfigureAwait(false);
                _firstCompleted = true;
            }

            public Task Logout(Settings settings) => UpdateToken(null, settings);

            async Task UpdateToken(PremiumAccessToken newToken, Settings settings) {
                var userInfo = settings.Secure.Login;
                var existingToken = userInfo.Authentication.PremiumToken;
                // Always process the first time. Then on consequtive, only update on change
                if (_firstCompleted &&
                    (newToken != null && newToken.Equals(existingToken) || (existingToken == null && newToken == null)))
                    return;
                userInfo.Authentication.PremiumToken = newToken;
                await new PremiumTokenUpdatedEvent(newToken).Raise().ConfigureAwait(false);
            }

            async Task<PremiumAccessToken> GetPremiumTokenInternal(string encryptedPremiumToken, string apiKey,
                Settings settings) {
                var premiumToken = settings.Secure.Login.Authentication.PremiumToken;
                try {
                    var premiumCached = _firstCompleted && premiumToken.IsPremium() &&
                                        premiumToken.IsValidInNearFuture();
                    if (!premiumCached) {
                        var aes = new Aes();
                        var sha1 = new SHA1Hash();

                        var keyHash = await sha1.GetHashAsync(apiKey).ConfigureAwait(false);
                        var unencryptedPremiumToken =
                            await aes.DecryptAsync(encryptedPremiumToken, keyHash).ConfigureAwait(false);
                        premiumToken = JsonConvert.DeserializeObject<PremiumAccessToken>(unencryptedPremiumToken);
                    }
                } catch (NotPremiumUserException) {
                    premiumToken = null;
                }
                return premiumToken;
            }
        }

        // TODO: Use the one included with Api.Models
        static class CustomClaimTypes
        {
            public const string PremiumToken = "withsix:premium_token";
            public const string EmailMd5 = "withsix:email_md5";
            public const string AvatarUrl = "withsix:avatar_url";
            public const string HasAvatar = "withsix:has_avatar";
            public const string AvatarUpdatedAt = "withsix:avatar_updated_at";
        }
    }

    public class PremiumEventHandler : IAsyncNotificationHandler<PremiumTokenUpdatedEvent>
    {
        readonly IAuthProvider _authProvider;

        public PremiumEventHandler(IAuthProvider authProvider) {
            _authProvider = authProvider;
        }

        public Task HandleAsync(PremiumTokenUpdatedEvent notification) {
#if DEBUG
            if (notification.NewToken != null) {
                MainLog.Logger.Debug("Premium UN: " + notification.NewToken.AccessToken);
                MainLog.Logger.Debug("Premium Token: " + notification.NewToken.PremiumKey);
            } else
                MainLog.Logger.Debug("Not Premium");
#endif
            return notification.NewToken == null ? RemovePremium() : AddPremium(notification.NewToken);
        }

        async Task RemovePremium() {
            foreach (var endpoint in Common.PremiumHosts)
                _authProvider.SetNonPersistentAuthInfo(("http://" + endpoint).ToUri(), null);
        }

        async Task AddPremium(PremiumAccessTokenV1 premiumToken) {
            foreach (var endpoint in Common.PremiumHosts) {
                _authProvider.SetNonPersistentAuthInfo(("http://" + endpoint).ToUri(),
                    new AuthInfo(premiumToken.AccessToken, premiumToken.PremiumKey));
            }
        }
    }
}
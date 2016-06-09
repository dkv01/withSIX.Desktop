// <copyright company="SIX Networks GmbH" file="TokenRefresher.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ShortBus;
using SN.withSIX.Api.Models.Exceptions;
using SN.withSIX.Core;
using SN.withSIX.Core.Applications.Infrastructure;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Core.Logging;
using SN.withSIX.Play.Core;
using SN.withSIX.Play.Core.Connect.Events;
using SN.withSIX.Play.Core.Options;
using Synercoding.Encryption.Hashing;
using Synercoding.Encryption.Symmetrical;

namespace SN.withSIX.Play.Infra.Api
{
    public interface ILoginHandler
    {
        //Task Logout();
        Task HandleLogin(AccessInfo info);
        Task ProcessLogin();
    }
    
    public class AccessInfo
    {
        public string AccessToken { get; set; }
    }

    public class LoginHandler : ILoginHandler, IInfrastructureService
    {
        readonly IOauthConnect _connect;
        readonly PremiumHandler _premiumRefresher;

        public LoginHandler(IOauthConnect connect) {
            _connect = connect;
            _premiumRefresher = new PremiumHandler();
        }

        bool _initial = false; // pff state

        public async Task HandleLogin(AccessInfo info) {
            var localUserInfo = DomainEvilGlobal.SecretData.UserInfo;
            if (_initial && info.AccessToken == localUserInfo.AccessToken)
                return;
            localUserInfo.AccessToken = info.AccessToken;
            // TODO: cleanup vs ContactList
            //await ProcessLogin().ConfigureAwait(false);
            await DomainEvilGlobal.SecretData.Save().ConfigureAwait(false);
            Common.App.PublishEvent(new ApiKeyUpdated(localUserInfo.AccessToken));
            _initial = true;
            //await new LoginChanged(localUserInfo).Raise().ConfigureAwait(false);
        }

        public async Task ProcessLogin() {
            var localUserInfo = DomainEvilGlobal.SecretData.UserInfo;
            if (localUserInfo.AccessToken != null)
                await TryHandleLoggedIn(localUserInfo).ConfigureAwait(false);
            else
                await HandleLoggedOut(localUserInfo).ConfigureAwait(false);
            await DomainEvilGlobal.SecretData.Save().ConfigureAwait(false);
        }

        private async Task TryHandleLoggedIn(UserInfo localUserInfo)
        {
            try
            {
                await HandleLoggedIn(localUserInfo).ConfigureAwait(false);
                // try fetch userinfo. if failed, consider logged out, perhaps ask the website for login again
            }
            catch (Exception ex)
            {
                MainLog.Logger.FormattedWarnException(ex, "Failure while processing login info");
                await HandleLoggedOut(localUserInfo).ConfigureAwait(false);
            }
        }

        private async Task HandleLoggedIn(UserInfo localUserInfo)
        {
            var userInfo =
                await
                    _connect.GetUserInfo(CommonUrls.AuthorizationEndpoints.UserInfoEndpoint,
                        localUserInfo.AccessToken)
                        .ConfigureAwait(false);
            localUserInfo.Account = BuildAccountInfo(userInfo);
            if (localUserInfo.Account.Roles.Contains("premium")) {
                await
                    _premiumRefresher.ProcessPremium(GetClaim(userInfo, CustomClaimTypes.PremiumToken))
                        .ConfigureAwait(false);
            }
        }

        private Task HandleLoggedOut(UserInfo localUserInfo)
        {
            localUserInfo.Account = new AccountInfo();
            return _premiumRefresher.Logout();
        }


        static AccountInfo BuildAccountInfo(IUserInfoResponse userInfo) {
            var avatarUrl = GetClaim(userInfo, CustomClaimTypes.AvatarUrl);
            var updatedAt = GetClaim(userInfo, CustomClaimTypes.AvatarUpdatedAt);
            return new AccountInfo {
                Id = Guid.Parse(GetClaim(userInfo, "sub")),
                Roles = userInfo.Claims.Where(x => x.Item1 == "role").Select(x => x.Item2).ToList(),
                DisplayName = GetClaim(userInfo, "nickname"),
                UserName = GetClaim(userInfo, "preferred_username"),
                AvatarURL = avatarUrl,
                HasAvatar = GetClaim(userInfo, CustomClaimTypes.HasAvatar) == "true",
                AvatarUpdatedAt = updatedAt == null ? 0 : long.Parse(updatedAt),
                EmailMd5 = GetClaim(userInfo, CustomClaimTypes.EmailMd5)
            };
        }

        static string GetClaim(IUserInfoResponse userInfo, string claimType) {
            var claim = userInfo.Claims.FirstOrDefault(x => x.Item1 == claimType);
            return claim?.Item2;
        }

        class PremiumHandler
        {
            bool _firstCompleted;

            public async Task ProcessPremium(string encryptedPremiumToken) {
                Contract.Requires<ArgumentNullException>(encryptedPremiumToken != null);
                Contract.Requires<ArgumentOutOfRangeException>(!string.IsNullOrEmpty(encryptedPremiumToken));
                // TODO
                //var apiKey = _connectionManager.ApiKey;
                //var apiKey = DomainEvilGlobal.Settings.AppOptions.Id.ToString().Sha256String();

                var apiKey = DomainEvilGlobal.SecretData.UserInfo.Account.Id.ToString();
                var premiumToken = await GetPremiumTokenInternal(encryptedPremiumToken, apiKey).ConfigureAwait(false);
                await UpdateToken(premiumToken).ConfigureAwait(false);
                _firstCompleted = true;
            }

            public Task Logout() => UpdateToken(null);

            async Task UpdateToken(PremiumAccessToken newToken) {
                var userInfo = DomainEvilGlobal.SecretData.UserInfo;
                var existingToken = userInfo.Token;
                // Always process the first time. Then on consequtive, only update on change
                if (_firstCompleted &&
                    (newToken != null && newToken.Equals(existingToken) || (existingToken == null && newToken == null)))
                    return;
                userInfo.Token = newToken;
                await Common.App.Mediator.NotifyAsync(new TokenUpdatedEvent(newToken)).ConfigureAwait(false);
            }

            async Task<PremiumAccessToken> GetPremiumTokenInternal(string encryptedPremiumToken, string apiKey) {
                var premiumToken = DomainEvilGlobal.Settings.AccountOptions.UserInfo.Token;
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
}
// <copyright company="SIX Networks GmbH" file="AuthProviderSettingsStorage.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.Core.Infra.Services;
using withSIX.Play.Core.Options;
using withSIX.Sync.Core.Transfer;

namespace withSIX.Play.Infra.Data.Services
{
    public class AuthProviderSettingsStorage : IAuthProviderStorage, IInfrastructureService
    {
        readonly UserSettings _settings;

        public AuthProviderSettingsStorage(UserSettings settings) {
            _settings = settings;
        }

        public void SetAuthInfo(Uri uri, AuthInfo authInfo) {
            _settings.AppOptions.SetAuthInfo(uri, authInfo);
        }

        public AuthInfo GetAuthInfoFromCache(Uri uri) => _settings.AppOptions.GetAuthInfoFromCache(uri);

        public Task<string> GetToken() => Task.FromResult(_settings.AccountOptions.UserInfo.AccessToken);
    }
}
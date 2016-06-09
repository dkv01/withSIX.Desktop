// <copyright company="SIX Networks GmbH" file="AuthProviderStorage.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SN.withSIX.Core.Infra.Services;
using SN.withSIX.Mini.Applications.Services.Infra;
using SN.withSIX.Sync.Core.Transfer;

namespace SN.withSIX.Mini.Infra.Data.Services
{
    // TODO: This is a dummy implementation, implement actual storage!
    public class AuthProviderSettingsStorage : IAuthProviderStorage, IInfrastructureService
    {
        readonly IDictionary<Uri, AuthInfo> _authInfos = new Dictionary<Uri, AuthInfo>();
        private readonly IDbContextLocator _locator;

        public AuthProviderSettingsStorage(IDbContextLocator locator) {
            _locator = locator;
        }

        public void SetAuthInfo(Uri uri, AuthInfo authInfo) {
            _authInfos[uri] = authInfo;
        }

        public AuthInfo GetAuthInfoFromCache(Uri uri) => _authInfos.ContainsKey(uri) ? _authInfos[uri] : null;

        public async Task<string> GetToken() {
            var sContext = _locator.GetSettingsContext();
            return (await sContext.GetSettings().ConfigureAwait(false)).Secure.Login?.Authentication.AccessToken;
        }
    }
}
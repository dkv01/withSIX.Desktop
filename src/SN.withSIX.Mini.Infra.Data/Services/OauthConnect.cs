// <copyright company="SIX Networks GmbH" file="OauthConnect.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using IdentityModel.Client;
using SN.withSIX.Core.Infra.Services;

namespace SN.withSIX.Mini.Infra.Data.Services
{
    public class OauthConnect : IInfrastructureService, IOauthConnect
    {
        /*
        public Uri GetLoginUri(Uri authorizationEndpoint, Uri callbackUri, string scope, string responseType,
            string clientName, string clientSecret) {
            var client = GetOAuthClient(authorizationEndpoint, clientName, clientSecret);
            //            client.RequestRefreshTokenAsync()
            return new Uri(client.CreateAuthorizeUrl(
                //clientId: "implicitclient",
                clientName, responseType, scope, callbackUri.ToString(), null, "random_nonce" /**,
                loginHint: "alice",
                acrValues: "idp:Google b c" **\/));
        }*/

        public async Task<TokenResponse> GetAuthorization(Uri tokenEndpoint, Uri callbackUrl, string code,
            string clientId, string clientSecret,
            Dictionary<string, string> additionalValues = null) {
            var client = GetOAuthClient(tokenEndpoint, clientId, clientSecret);
            var response =
                await
                    client.RequestAuthorizationCodeAsync(code, callbackUrl.ToString(), extra: additionalValues)
                        .ConfigureAwait(false);
            if (response.IsError) {
                throw new Exception(
                    $"Error while retrieving authorization: {response.Error}. Code: {response.HttpStatusCode}. Reason: {response.HttpErrorReason}");
            }
            return new TokenResponse(response.Raw);
        }

        public async Task<TokenResponse> RefreshToken(Uri tokenEndpoint, string refreshToken, string clientId,
            string clientSecret,
            Dictionary<string, string> additionalValues = null) {
            var client = GetOAuthClient(tokenEndpoint, clientId, clientSecret);
            var response = await client.RequestRefreshTokenAsync(refreshToken, additionalValues).ConfigureAwait(false);
            if (response.IsError) {
                if (response.Error == "invalid_grant")
                    throw new RefreshTokenInvalidException(response.Error);
                throw new Exception(
                    $"Error while refreshing token: {response.Error}. Code: {response.HttpStatusCode}. Reason: {response.HttpErrorReason}");
            }
            return new TokenResponse(response.Raw);
        }

        public async Task<UserInfoResponse> GetUserInfo(Uri userInfoEndpoint, string accessToken) {
            var userInfoClient = new UserInfoClient(userInfoEndpoint.ToString());
            var response = await userInfoClient.GetAsync(accessToken).ConfigureAwait(false);
            if (response.IsError || response.IsHttpError)
                throw new Exception(
                    $"Error while retrieving userinfo: {response.Error} {response.HttpStatusCode} {response.HttpErrorReason}");
            return new UserInfoResponse(response.Raw);
        }

        public AuthorizeResponse GetResponse(Uri callbackUri, Uri currentUri) {
            if (!currentUri.ToString().StartsWith(callbackUri.AbsoluteUri))
                throw new Exception("Not valid callback uri");
            return new AuthorizeResponse(currentUri.AbsoluteUri);
        }

        static TokenClient GetOAuthClient(Uri endpoint, string clientId, string clientSecret)
            => new TokenClient(endpoint.ToString(), clientId, clientSecret);
    }

    public class RefreshTokenInvalidException : Exception
    {
        public RefreshTokenInvalidException(string error) : base(error) {}
    }
}
﻿// <copyright company="SIX Networks GmbH" file="IAuthProvider.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;

namespace withSIX.Sync.Core.Transfer
{
    public interface IAuthProvider
    {
        AuthInfo GetAuthInfoFromUriWithCache(Uri uri);
        void SetNonPersistentAuthInfo(Uri uri, AuthInfo authInfo);
        Uri HandleUriAuth(Uri uri, string username = null, string password = null);
        Uri HandleUri(Uri uri);
        AuthInfo GetAuthInfoFromUri(Uri uri);
        Task<string> GetToken();
        void HandleAuthInfo(Uri uri, IWebClient client);
    }
}
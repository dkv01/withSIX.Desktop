// <copyright company="SIX Networks GmbH" file="ApiKeyUpdated.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace withSIX.Play.Core.Connect.Events
{
    public class ApiKeyUpdated : EventArgs
    {
        public string ApiKey { get; }

        public ApiKeyUpdated(string apiKey) {
            ApiKey = apiKey;
        }
    }
}
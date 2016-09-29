// <copyright company="SIX Networks GmbH" file="ICallContextService.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Core.Applications.Services
{
    public interface ICallContextService
    {
        object GetIdentifier(string key);
        void SetId(object id, string key);
        object Create();
    }
}
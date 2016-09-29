// <copyright company="SIX Networks GmbH" file="IContentEngineGameContext.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.ContentEngine.Core;

namespace withSIX.ContentEngine.Infra.Services
{
    public interface IContentEngineGameContext
    {
        IContentEngineGame Get(Guid gameId);
    }
}
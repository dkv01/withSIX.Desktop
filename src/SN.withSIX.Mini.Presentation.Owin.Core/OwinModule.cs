// <copyright company="SIX Networks GmbH" file="OwinModule.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using Microsoft.AspNetCore.Builder;

namespace withSIX.Mini.Presentation.Owin.Core
{
    public abstract class OwinModule
    {
        public abstract void Configure(IApplicationBuilder builder);
    }
}
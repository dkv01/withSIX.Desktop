// <copyright company="SIX Networks GmbH" file="IBridge.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using Newtonsoft.Json;

namespace withSIX.Core.Presentation
{
    public interface IBridge
    {
        JsonSerializerSettings GameContextSettings();
    }
}
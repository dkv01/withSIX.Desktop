﻿// <copyright company="SIX Networks GmbH" file="ModDto.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace SN.withSIX.Mini.Infra.Api.WebApi
{
    public class ModDtoV2 : ContentDtoV2
    {
        //public string CppName { get; set; }
        public string LatestStableVersion { get; set; }
        public string GetVersion() => LatestStableVersion ?? Version;
    }
}
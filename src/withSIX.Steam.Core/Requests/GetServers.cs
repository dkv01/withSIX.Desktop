﻿// <copyright company="SIX Networks GmbH" file="GetServers.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace withSIX.Steam.Core.Requests
{
    public class GetServers : GetServerAddresses
    {
        public bool IncludeRules { get; set; }
        public bool IncludeDetails { get; set; }
    }

    public class GetServerAddresses
    {
        [Required]
        public List<Tuple<string, string>> Filter { get; set; }
        public int PageSize { get; set; } = 24;
    }
}
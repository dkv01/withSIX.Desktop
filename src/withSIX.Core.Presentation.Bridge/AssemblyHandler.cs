﻿// <copyright company="SIX Networks GmbH" file="AssemblyHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;

namespace withSIX.Core.Presentation.Bridge
{
    public class AssemblyHandler
    {
        public static readonly string Bitness = Environment.Is64BitProcess ? "x64" : "x86";

        public void Register() {
            var path = CommonBase.AssemblyLoader.GetNetEntryPath().GetChildDirectoryWithName(Bitness);
            Environment.SetEnvironmentVariable("path",
                string.Join(";", path,
                    Environment.GetEnvironmentVariable("path")));
        }
    }
}
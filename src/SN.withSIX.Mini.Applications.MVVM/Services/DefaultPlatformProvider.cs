﻿// <copyright company="SIX Networks GmbH" file="DefaultPlatformProvider.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Mini.Applications.MVVM.Services
{
    public class DefaultPlatformProvider : IPlatformProvider
    {
        public bool InDesignMode => true;
    }
}
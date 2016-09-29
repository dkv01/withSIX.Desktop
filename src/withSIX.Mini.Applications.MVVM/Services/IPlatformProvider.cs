// <copyright company="SIX Networks GmbH" file="IPlatformProvider.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace withSIX.Mini.Applications.MVVM.Services
{
    public interface IPlatformProvider
    {
        bool InDesignMode { get; }
    }
}
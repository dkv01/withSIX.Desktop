// <copyright company="SIX Networks GmbH" file="ISquirrelApp.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using Squirrel;

namespace SN.withSIX.Mini.Presentation.Wpf.Services
{
    public interface ISquirrelApp
    {
        Task<string> GetNewVersion();
    }

    public interface ISquirrelUpdater
    {
        Task<UpdateInfo> CheckForUpdates();
        Task<ReleaseEntry> UpdateApp(Action<int> progressAction);
    }
}
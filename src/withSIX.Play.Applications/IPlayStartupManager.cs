// <copyright company="SIX Networks GmbH" file="IPlayStartupManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;

namespace withSIX.Play.Applications
{
    public interface IPlayStartupManager : IWpfStartupManager
    {
        void HandleSoftwareUpdate();
        void RegisterUrlHandlers();
        //void RegisterOnline();
        void RegisterUserAppKeys();
        void ClearAwesomiumCache();
        void StartAwesomium();
        void FirstTimeLicenseDialog(object obj);
        Task LaunchSignalr();
        string GetSecurityWarning();
        Task VisualInit();
    }
}
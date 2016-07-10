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
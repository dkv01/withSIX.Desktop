// <copyright company="SIX Networks GmbH" file="ConsoleAppBootstrapper.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.Core.Applications.Services;
using withSIX.Core.Presentation.Bridge;

namespace withSIX.Core.Presentation.Wpf.Legacy
{
    public abstract class ConsoleAppBootstrapper : AppBootstrapperBase
    {
        protected ConsoleAppBootstrapper() : base(false) {}

        protected override void ConfigureContainer() {
            base.ConfigureContainer();
            Container.Register<IShutdownHandler, ShutdownHandler>();
            Container.Register<IFirstTimeLicense, ConsoleFirstTimeLicense>();
            Container.Register<IDialogManager, ConsoleDialogManager>();
        }

        public virtual int OnStartup(params string[] args) {
            SetupContainer();
            PreStart();
            return 0;
        }

        protected abstract void PreStart();
    }

    public class ConsoleDialogManager : IDialogManager
    {
        public Task<string> BrowseForFolder(string selectedPath = null, string title = null) {
            throw new NotImplementedException();
        }

        public Task<string> BrowseForFile(string initialDirectory = null, string title = null, string defaultExt = null,
            bool checkFileExists = true) {
            throw new NotImplementedException();
        }

        public Task<SixMessageBoxResult> MessageBox(MessageBoxDialogParams dialogParams) {
            throw new NotImplementedException();
        }

        public Task<bool> ExceptionDialog(Exception e, string message, string title = null, object owner = null) {
            throw new NotImplementedException();
        }
    }

    public abstract class ConsoleAppBootstrapper<T> : ConsoleAppBootstrapper where T : class, IConsoleLauncher
    {
        public override int OnStartup(params string[] args) {
            base.OnStartup(args);
            return Container.GetInstance<T>().Run(args);
        }
    }

    public interface IConsoleLauncher
    {
        int Run(params string[] args);
    }
}
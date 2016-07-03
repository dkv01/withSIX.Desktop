using System;
using System.Threading.Tasks;
using ReactiveUI;
using SN.withSIX.Mini.Applications;
using SN.withSIX.Mini.Applications.Services;

namespace SN.withSIX.Mini.Presentation.Wpf.Services
{
    public class NullNodeApi : INodeApi
    {
        public string Version { get; }
        public ArgsO Args { get; }

        public Task<RecoveryOptionResult> HandleUserError(UserError error) {
            throw new NotImplementedException();
        }

        public Task<string> ShowMessageBox(string title, string message, string[] buttons, string type = null) {
            throw new NotImplementedException();
        }

        public Task<string> ShowSaveDialog(string title = null, string defaultPath = null) {
            throw new NotImplementedException();
        }

        public Task<string[]> ShowFileDialog(string title = null, string defaultPath = null) {
            throw new NotImplementedException();
        }

        public Task<string[]> ShowFolderDialog(string title = null, string defaultPath = null) {
            throw new NotImplementedException();
        }

        public Task DisplayTrayBaloon(string title, string content, string icon = null) {
            throw new NotImplementedException();
        }

        public Task SetState(BusyState state, string description, double? progress) {
            throw new NotImplementedException();
        }

        public Task InstallSelfUpdate() {
            throw new NotImplementedException();
        }

        public Task Exit(int exitCode) {
            throw new NotImplementedException();
        }
    }
}
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;

namespace withSIX.Core.Presentation.WinRT
{
    public class Initializer : IInitializer
    {
        public Task Initialize() {
            // TODO
            UiTaskHandler.RegisterCommand = (command, action) => {
                // ThrownExceptions does not listen to Subscribe errors, but only in async task errors!
                command.ThrownExceptions
                    .Select(x => ErrorHandlerr.HandleException(x, action))
                    .SelectMany(UserError.Throw)
                    .Where(x => x == RecoveryOptionResult.RetryOperation)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .InvokeCommand(command);
            };
            return TaskExt.Default;
        }

        public Task Deinitialize() => TaskExt.Default;
    }
}

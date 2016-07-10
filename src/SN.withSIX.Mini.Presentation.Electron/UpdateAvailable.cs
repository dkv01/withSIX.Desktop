using System.Threading.Tasks;
using SN.withSIX.Mini.Applications.Extensions;
using SN.withSIX.Mini.Applications.Usecases.Main;

namespace SN.withSIX.Mini.Presentation.Electron
{
    public class UpdateAvailable : IUsecaseExecutor
    {
        public async Task<object> Invoke(dynamic input) {
            var state = (UpdateState)(int)input.state;
            var version = input.version as string;
            await
                this.RequestAsync(new Applications.Usecases.Main.UpdateAvailable(state, version))
                    .ConfigureAwait(false);
            return true;
        }
    }
}
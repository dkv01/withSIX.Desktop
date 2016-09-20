using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using SN.withSIX.Core.Applications.Services;

namespace SN.withSIX.Mini.Presentation.Electron
{
    public class NodeNotifier : INotificationProvider
    {
        private readonly INodeApi _api;

        public NodeNotifier(INodeApi api) {
            _api = api;
        }

        public async Task<bool?> Notify(string subject, string text, string icon = null, TimeSpan? expirationTime = null,
            params TrayAction[] actions) {
            var r = await _api.ShowNotification(subject, text).ConfigureAwait(false);
            if (r.GetValueOrDefault(false)) {
                if (actions.Any())
                    await actions.First().Command();
            }
            return r;
        }
    }
}
using System;
using System.Threading;

namespace withSIX.Play.Core.Games.Services
{
    public class StatusRepo : SN.withSIX.Sync.Core.Legacy.Status.StatusRepo, IDisposable
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public StatusRepo() {
            CancelToken = _cts.Token;
        }

        public void Dispose() {
            _cts.Dispose();
        }

        public void Abort() {
            _cts.Cancel();
        }
    }
}
using System;
using System.Threading;

namespace SN.withSIX.Play.Core.Games.Services
{
    public class StatusRepo : SN.withSIX.Sync.Core.Legacy.Status.StatusRepo, IDisposable
    {
        private CancellationTokenSource _cts;

        public StatusRepo() : base() {
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
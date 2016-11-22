// <copyright company="SIX Networks GmbH" file="AsyncLock.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace withSIX.Core.Helpers
{
    public sealed class AsyncLock : IDisposable
    {
        private readonly SemaphoreSlim _mSemaphore = new SemaphoreSlim(1, 1);
        private readonly IDisposable _releaser;

        public AsyncLock() {
            _releaser = new Releaser(this);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AsyncLock() {
            Dispose(false);
        }

        protected void Dispose(bool disposing) {
            if (disposing) {
                _mSemaphore.Dispose();
            }
        }

        public Task<IDisposable> LockAsync() {
            var wait = _mSemaphore.WaitAsync();
            return Wait(wait);
        }

        public Task<IDisposable> LockAsync(CancellationToken token) {
            var wait = _mSemaphore.WaitAsync(token);
            return Wait(wait);
        }

        private async Task<IDisposable> Wait(Task wait) {
            if (wait.IsCompleted)
                return _releaser;
            await wait.ConfigureAwait(false);
            return _releaser;
        }

        public IDisposable Lock() {
            _mSemaphore.Wait();
            return _releaser;
        }

        private void Release() => _mSemaphore.Release();

        private sealed class Releaser : IDisposable
        {
            private readonly AsyncLock m_toRelease;

            internal Releaser(AsyncLock toRelease) {
                m_toRelease = toRelease;
            }

            public void Dispose() {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected void Dispose(bool disposing) {
                // We always call this, because we always want to release the lock - the Finalizer won't help us here
                m_toRelease.Release();
            }

            ~Releaser() {
                Dispose(false);
            }
        }
    }

    public class Disposable : IDisposable
    {
        private readonly Action _dispose;

        public Disposable(Action dispose) {
            _dispose = dispose;
        }

        public void Dispose() {
            _dispose();
        }
    }
}
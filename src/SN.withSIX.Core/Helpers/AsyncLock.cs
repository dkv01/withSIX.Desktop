// <copyright company="SIX Networks GmbH" file="AsyncLock.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace SN.withSIX.Core.Helpers
{
    public sealed class AsyncLock : IDisposable
    {
        private readonly IDisposable _releaser;
        private readonly Task<IDisposable> m_releaser;
        private readonly SemaphoreSlim m_semaphore = new SemaphoreSlim(1, 1);

        public AsyncLock() {
            _releaser = new Releaser(this);
            m_releaser = Task.FromResult(_releaser);
        }

        public void Dispose() {
            m_semaphore.Dispose();
        }

        public Task<IDisposable> LockAsync() {
            var wait = m_semaphore.WaitAsync();
            return wait.IsCompleted
                ? m_releaser
                : wait.ContinueWith((_, state) => (IDisposable) state,
                    _releaser, CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public Task<IDisposable> LockAsync(CancellationToken token) {
            var wait = m_semaphore.WaitAsync(token);
            return wait.IsCompleted
                ? m_releaser
                : wait.ContinueWith((_, state) => (IDisposable) state,
                    _releaser, CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public IDisposable Lock() {
            m_semaphore.Wait();
            return _releaser;
        }

        private void Release() {
            m_semaphore.Release();
        }

        private sealed class Releaser : IDisposable
        {
            private readonly AsyncLock m_toRelease;

            internal Releaser(AsyncLock toRelease) {
                m_toRelease = toRelease;
            }

            public void Dispose() {
                m_toRelease.Release();
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
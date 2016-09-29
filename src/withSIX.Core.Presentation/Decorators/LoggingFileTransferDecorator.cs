// <copyright company="SIX Networks GmbH" file="LoggingFileTransferDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.Core.Logging;
using withSIX.Sync.Core.Transfer.Specs;

namespace withSIX.Core.Presentation.Decorators
{
    public abstract class LoggingFileTransferDecorator : IEnableLogging
    {
        protected abstract void OnFinished(TransferSpec spec);
        protected abstract void OnError(TransferSpec spec, Exception exception);
        protected abstract void OnStart(TransferSpec spec);

        protected Task Wrap(Func<Task> task, TransferSpec spec)
            => Common.Flags.Verbose ? WrapInternal(task, spec) : task();

        private async Task WrapInternal(Func<Task> task, TransferSpec spec) {
            OnStart(spec);
            try {
                await task().ConfigureAwait(false);
            } catch (Exception e) {
                OnError(spec, e);
                throw;
            }
            OnFinished(spec);
        }

        protected void Wrap(Action action, TransferSpec spec) {
            if (Common.Flags.Verbose)
                WrapInternal(action, spec);
            else
                action();
        }

        private void WrapInternal(Action action, TransferSpec spec) {
            OnStart(spec);
            try {
                action();
            } catch (Exception e) {
                OnError(spec, e);
                throw;
            }
            OnFinished(spec);
        }
    }
}
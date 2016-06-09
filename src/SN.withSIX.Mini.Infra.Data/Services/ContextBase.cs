// <copyright company="SIX Networks GmbH" file="ContextBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using SN.withSIX.Core.Applications.Infrastructure;
using SN.withSIX.Core.Logging;
using SN.withSIX.Core.Services.Infrastructure;

namespace SN.withSIX.Mini.Infra.Data.Services
{
    public abstract class ContextBase : IUnitOfWork
    {
        readonly ICollection<Action> _transactionCallbacks = new Collection<Action>();
        readonly ICollection<Func<Task>> _transactionCallbacksAsync = new Collection<Func<Task>>();
        public IDomainEventHandler DomainEventHandler { get; } = new DefaultDomainEventHandler();

        public async Task<int> SaveChanges() {
            using (this.Bench()) {
                await RaiseEvents().ConfigureAwait(false);
                var changes = await SaveChangesInternal().ConfigureAwait(false);
                await ExecuteTransactionCallbacks().ConfigureAwait(false);
                return changes;
            }
        }

        public void AddTransactionCallback(Action act) => _transactionCallbacks.Add(act);

        public void AddTransactionCallback(Func<Task> act) => _transactionCallbacksAsync.Add(act);

        async Task ExecuteTransactionCallbacks() {
            using (this.Bench()) {
                var callbacks = _transactionCallbacks.ToArray();
                _transactionCallbacks.Clear();

                var asyncCallbacks = _transactionCallbacksAsync.ToArray();
                _transactionCallbacksAsync.Clear();

                foreach (var c in callbacks)
                    c();

                foreach (var c in asyncCallbacks)
                    await c().ConfigureAwait(false);
            }
        }

        async Task RaiseEvents() {
            using (this.Bench())
                await DomainEventHandler.RaiseEvents().ConfigureAwait(false);
        }

        protected abstract Task<int> SaveChangesInternal();
    }

    public abstract class ContextBase<T> : ContextBase
    {
        readonly Lazy<Task<T>> _lazy;
        private T _loaded;

        protected ContextBase() {
            _lazy = new Lazy<Task<T>>(Load);
        }

        private async Task<T> Load() {
            using (this.Bench())
                return _loaded = await LoadInternal().ConfigureAwait(false);
        }

        protected abstract Task<T> LoadInternal();

        protected Task<T> Get() => _lazy.Value;

        protected sealed override async Task<int> SaveChangesInternal() {
            using (this.Bench()) {
                if (_loaded == null)
                    return 0;
                await SaveChangesInternal(_loaded).ConfigureAwait(false);
                return 1;
            }
        }

        protected abstract Task SaveChangesInternal(T loaded);
    }
}
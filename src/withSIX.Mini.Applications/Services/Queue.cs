// <copyright company="SIX Networks GmbH" file="Queue.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using withSIX.Api.Models.Content.v3;
using withSIX.Api.Models.Exceptions;
using withSIX.Core.Applications.Errors;
using withSIX.Core.Applications.Services;
using withSIX.Core.Extensions;
using withSIX.Mini.Applications.Services.Infra;
using withSIX.Sync.Core.Legacy.Status;

namespace withSIX.Mini.Applications.Services
{
    public class QueueInfo
    {
        // TODO: concurrent?
        public List<QueueItem> Items { get; protected set; } = new List<QueueItem>();
    }

    public class QueueItem : IHaveId<Guid>
    {
        public QueueItem(string title, Guid contentId, Func<Action<ProgressState>, CancellationToken, Task> taskFactory) {
            Title = title;
            ContentId = contentId;
            TaskFactory = taskFactory;
        }

        public Func<Action<ProgressState>, CancellationToken, Task> TaskFactory { get; protected set; }

        public Guid Id { get; protected set; } = Guid.NewGuid();
        public Guid ContentId { get; }
        public string Title { get; protected set; }
        public ProgressState ProgressState { get; set; }
        public CompletionState State { get; protected set; }

        public DateTime Created { get; protected set; } = DateTime.UtcNow;
        public DateTime? Finished { get; protected set; }

        [JsonIgnore]
        public CancellationTokenSource CancelToken { get; protected internal set; } = new CancellationTokenSource();

        [JsonIgnore]
        public Task Task { get; set; }

        public void Start(Action stateUpdated) {
            Task = TaskFactory(ps => {
                if (ps.Equals(ProgressState))
                    return;
                ProgressState = ps;
                stateUpdated();
            }, CancelToken.Token);
        }

        public void UpdateState(CompletionState state) {
            State = state;
            Finished = DateTime.UtcNow;
            CancelToken.Dispose();
            CancelToken = null;
            TaskFactory = null;
            Task = null;
        }

        public void Cancel() {
            if (CancelToken == null)
                throw new ValidationException("Not cancelable");
            CancelToken.Cancel();
            State = CompletionState.Canceled; // handled by the manager instead?
        }

        public void Retry(Action stateUpdated) {
            var cts = new CancellationTokenSource();
            CancelToken = cts;
            State = CompletionState.NotComplete;
            Start(stateUpdated);
        }
    }

    public class ProgressState : IEquatable<ProgressState>
    {
        public ProgressState(double progress, long? speed, string action) {
            Progress = progress;
            Speed = speed;
            Action = action;
        }

        public double Progress { get; }
        public long? Speed { get; }

        public string Action { get; }
        public DateTime LastUpdate { get; protected set; } = DateTime.UtcNow;

        public bool Equals(ProgressState other) => other != null && other.GetHashCode() == GetHashCode();

        public override bool Equals(object other) {
            var o = other as ProgressState;
            return o != null && Equals(o);
        }

        public override int GetHashCode() => HashCode.Start.Hash(Progress).Hash(Action).Hash(Speed);
    }

    public enum CompletionState
    {
        NotComplete,
        Success,
        Failure,
        Canceled
    }

    public class QueueUpdate
    {
        public Guid Id { get; set; }
        public QueueItem Item { get; set; }
    }

    public class QueueManager : IApplicationService, IQueueManager
    {
        private readonly IQueueHubMessenger _messenger;

        public QueueManager(IQueueHubMessenger messenger) {
            _messenger = messenger;
        }

        // TODO: progress handling
        public async Task<Guid> AddToQueue(string title, Guid contentId,
            Func<Action<ProgressState>, CancellationToken, Task> taskFactory) {
            var item = new QueueItem(title, contentId, taskFactory);

            item.Start(() => _messenger.Update(item));
            BuildContinuation(item);

            Queue.Items.Add(item);
            await _messenger.AddToQueue(item).ConfigureAwait(false);
            return item.Id;
        }

        public Task RemoveFromQueue(Guid id) {
            var item = Queue.Items.FindOrThrow(id);
            if (item.State == CompletionState.NotComplete)
                throw new ValidationException("Item is not in completed state");
            Queue.Items.Remove(item);
            return _messenger.RemoveFromQueue(id);
        }

        public Task Cancel(Guid id) => CancelInternal(Queue.Items.FindOrThrow(id));

        public Task Retry(Guid id) {
            var item = Queue.Items.FindOrThrow(id);
            if (item.State == CompletionState.NotComplete)
                throw new ValidationException("Item is not in completed state");
            item.Retry(() => _messenger.Update(item));
            BuildContinuation(item);
            return _messenger.Update(item);
        }

        public Task Update(QueueItem item) => _messenger.Update(item);

        public QueueInfo Queue { get; } = new QueueInfo();

        public Task CancelByContentId(Guid contentId)
            => CancelInternal(Queue.Items.FirstOrThrow(x => x.ContentId == contentId));

        private Task CancelInternal(QueueItem item) {
            if (item.State != CompletionState.NotComplete)
                throw new ValidationException("Item is not in progress state");
            item.Cancel();
            return _messenger.Update(item);
        }

        private void BuildContinuation(QueueItem item) {
            item.Task = BuildContinuationInternal(item);
        }

        private async Task BuildContinuationInternal(QueueItem item) {
            try {
                await item.Task.ConfigureAwait(false);
            } catch (OperationCanceledException) {
                item.UpdateState(CompletionState.Canceled);
                // Handled by item already?? However I suppose we can have multiple sources of cancellation (user induced, or system induced etc)
                await Update(item).ConfigureAwait(false); // Handled by Cancel method in manager already?
                return;
            } catch (Exception ex) {
                if (await HandleError(item, ex).ConfigureAwait(false))
                    return;
                item.UpdateState(CompletionState.Failure);
                await Update(item).ConfigureAwait(false);
                throw; // not sure..
            }
            item.UpdateState(CompletionState.Success);
            await Update(item).ConfigureAwait(false);
        }

        private async Task<bool> HandleError(QueueItem item, Exception ex) {
            var result =
                await UserErrorHandler.HandleUserError(ErrorHandlerr.HandleException(ex, "Queue action: " + item.Title));
            switch (result) {
            case RecoveryOptionResultModel.RetryOperation:
                item.Retry(() => _messenger.Update(item));
                BuildContinuation(item);
                return true;
            case RecoveryOptionResultModel.CancelOperation:
                item.UpdateState(CompletionState.Canceled);
                await Update(item).ConfigureAwait(false);
                return true;
            }
            return false;
        }
    }
}
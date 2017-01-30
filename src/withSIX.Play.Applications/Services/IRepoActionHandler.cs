// <copyright company="SIX Networks GmbH" file="IRepoActionHandler.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using withSIX.Play.Core.Games.Services;

namespace withSIX.Play.Applications.Services
{
    [ContractClass(typeof (RepoHandlerContract))]
    public interface IRepoActionHandler
    {
        StatusMod ActiveStatusMod { get; set; }
        void PerformStatusActionWithBusyHandling(StatusRepo repo, string actionText, Action act);
        void PerformStatusAction(string actionText, Action<StatusRepo> act);
        Task PerformStatusActionWithBusyHandlingAsync(StatusRepo repo, string actionText, Func<Task> act);
        Task PerformStatusActionAsync(string actionText, Func<StatusRepo, Task> act);
        Task PerformUpdaterActionSuspendedAsync(string actionText, Func<Task> act);
        Task TryUpdaterActionAsync(Func<Task> action, string task);
        Task<bool> DealWithUpdateException(Exception e, string task, string message, string title);
    }

    [ContractClassFor(typeof (IRepoActionHandler))]
    public abstract class RepoHandlerContract : IRepoActionHandler
    {
        public void PerformStatusActionWithBusyHandling(StatusRepo repo, string actionText, Action act) {
            if (repo == null) throw new ArgumentNullException(nameof(repo));
            if (actionText == null) throw new ArgumentNullException(nameof(actionText));
            if (act == null) throw new ArgumentNullException(nameof(act));
        }

        public void PerformStatusAction(string actionText, Action<StatusRepo> act) {
            if (actionText == null) throw new ArgumentNullException(nameof(actionText));
            if (act == null) throw new ArgumentNullException(nameof(act));
        }

        public Task PerformStatusActionWithBusyHandlingAsync(StatusRepo repo, string actionText, Func<Task> act) {
            if (repo == null) throw new ArgumentNullException(nameof(repo));
            if (actionText == null) throw new ArgumentNullException(nameof(actionText));
            if (act == null) throw new ArgumentNullException(nameof(act));
            return default(Task);
        }

        public Task PerformStatusActionAsync(string actionText, Func<StatusRepo, Task> act) {
            if (actionText == null) throw new ArgumentNullException(nameof(actionText));
            if (act == null) throw new ArgumentNullException(nameof(act));
            return default(Task);
        }

        public Task PerformUpdaterActionSuspendedAsync(string actionText, Func<Task> act) {
            if (actionText == null) throw new ArgumentNullException(nameof(actionText));
            if (act == null) throw new ArgumentNullException(nameof(act));
            return default(Task);
        }

        public Task TryUpdaterActionAsync(Func<Task> action, string task) {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (task == null) throw new ArgumentNullException(nameof(task));
            return default(Task);
        }

        public Task<bool> DealWithUpdateException(Exception e, string task, string message, string title) {
            if (e == null) throw new ArgumentNullException(nameof(e));
            return Task.FromResult(default(bool));
        }

        public abstract StatusMod ActiveStatusMod { get; set; }
    }
}
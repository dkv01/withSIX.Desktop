using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using withSIX.Core.Infra.Services;
using withSIX.Core.Presentation;
using withSIX.Mini.Applications.Services;

namespace withSIX.Mini.Presentation.Owin.Core
{
    public static class A
    {
        // Static because new commands create new Hub instances
        public static readonly CancellationTokenMapping CancellationTokenMapping = new CancellationTokenMapping();
        public static readonly Excecutor Excecutor = new Excecutor();

        public static async Task<TResponse> ApiAction<TResponse>(Func<CancellationToken, Task<TResponse>> action, object command,
            Func<string, Exception, Exception> createException, Guid requestId, string connectionId, IPrincipal user,
            IRequestScopeService scope) {
            var ct = CancellationTokenMapping.AddToken(requestId);
            try {
                using (scope.StartScope(connectionId, requestId, user, ct))
                    return await Excecutor.ApiAction(() => action(ct), command, createException).ConfigureAwait(false);
            } finally {
                CancellationTokenMapping.Remove(requestId);
            }
        }

        public static async Task ApiAction(Func<CancellationToken, Task> action, object command,
            Func<string, Exception, Exception> createException, Guid requestId, string connectionId, IPrincipal user,
            IRequestScopeService scope) {
            var ct = CancellationTokenMapping.AddToken(requestId);
            try {
                using (scope.StartScope(connectionId, requestId, user, ct))
                    await Excecutor.ApiAction(() => action(ct), command, createException).ConfigureAwait(false);
            } finally {
                CancellationTokenMapping.Remove(requestId);
            }
        }
    }

}

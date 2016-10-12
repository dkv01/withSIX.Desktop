using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace withSIX.Core.Applications.Services
{
    public interface IRequestScopeLocator
    {
        IRequestScope Scope { get; }
    }

    public interface IRequestScope
    {
        string ConnectionId { get; }
        Guid RequestId { get; }
        CancellationToken CancelToken { get; }
        IPrincipal User { get; }
        void SendMessage<T>(T message, string contract = null);
    }
}

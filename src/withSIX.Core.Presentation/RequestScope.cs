// <copyright company="SIX Networks GmbH" file="RequestScope.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections;
using System.Security.Principal;
using System.Threading;
using SimpleInjector;
using SimpleInjector.Extensions.ExecutionContextScoping;
using withSIX.Core.Applications.Services;
using withSIX.Core.Presentation.Services;

namespace withSIX.Core.Presentation
{
    public class RequestScope : IRequestScope
    {
        private readonly IMessageBusProxy _mb;
        public string ConnectionId { get; private set; }
        public Guid RequestId { get; private set; }

        public CancellationToken CancelToken { get; private set; }
        public IPrincipal User { get; private set; }

        public RequestScope(IMessageBusProxy mb) {
            _mb = mb;
        }

        public void SendMessage<T>(T message, string contract = null) => _mb.SendMessage(Tuple.Create<IRequestScope, T>(this, message), contract);

        internal void Load(string connectionId, Guid requestId, IPrincipal user) {
            ConnectionId = connectionId;
            RequestId = requestId;
            User = user;
        }

        internal void Load(string connectionId, Guid requestId, IPrincipal user, CancellationToken ct) {
            Load(connectionId, requestId, user);
            CancelToken = ct;
        }
    }

    public interface IRequestScopeService
    {
        IDisposable StartScope(string connectionId, Guid requestId, IPrincipal user);
        IDisposable StartScope(string connectionId, Guid requestId, IPrincipal user, CancellationToken ct);
    }

    public class RequestScopeService : IRequestScopeService, IRequestScopeLocator
    {
        private readonly Container _container;

        public RequestScopeService(Container container) {
            _container = container;
        }

        public static IRequestScopeService Instance { get; set; }

        public IRequestScopeLocator Locator => this;

        public IRequestScope Scope => _container.GetInstance<IRequestScope>();

        public IDisposable StartScope(string connectionId, Guid requestId, IPrincipal user) {
            var thes = BeginScope();
            try {
                var s = (RequestScope) Scope;
                s.Load(connectionId, requestId, user);
                return thes;
            } catch (Exception) {
                thes.Dispose();
                throw;
            }
        }

        public IDisposable StartScope(string connectionId, Guid requestId, IPrincipal user, CancellationToken ct) {
            var thes = BeginScope();
            try {
                var s = (RequestScope)Scope;
                s.Load(connectionId, requestId, user, ct);
                return thes;
            } catch (Exception) {
                thes.Dispose();
                throw;
            }
        }

        IDisposable BeginScope() => _container.BeginExecutionContextScope();
    }
}
// <copyright company="SIX Networks GmbH" file="MediatorApiSessionDecorator.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SN.withSIX.Core.Applications.Extensions;
using SN.withSIX.Play.Core.Connect.Infrastructure;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Play.Applications
{
    public class MediatorApiContextDecorator : MediatorDecoratorBase
    {
        readonly IConnectApiHandler _scopeFactory;

        public MediatorApiContextDecorator(IMediator mediator, IConnectApiHandler scopeFactory) : base(mediator) {
            Contract.Requires<ArgumentNullException>(scopeFactory != null);
            _scopeFactory = scopeFactory;
        }

        public override TResponseData Send<TResponseData>(IRequest<TResponseData> request) {
            if (!(request is IRequireApiSession))
                return base.Send(request);
            using (var scope = _scopeFactory.StartSession().Result) {
                var response = base.Send(request);
                scope.Close().WaitAndUnwrapException();
                return response;
            }
        }

        public override async Task<TResponseData> SendAsync<TResponseData>(IAsyncRequest<TResponseData> request) {
            if (!(request is IRequireApiSession))
                return await base.SendAsync(request).ConfigureAwait(false);
            using (var scope = await _scopeFactory.StartSession().ConfigureAwait(false)) {
                var response = await base.SendAsync(request).ConfigureAwait(false);
                await scope.Close().ConfigureAwait(false);
                return response;
            }
        }

        public override async Task<TResponse> SendAsync<TResponse>(ICancellableAsyncRequest<TResponse> request,
            CancellationToken cancellationToken) {
            if (!(request is IRequireApiSession))
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            using (var scope = await _scopeFactory.StartSession().ConfigureAwait(false)) {
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await scope.Close().ConfigureAwait(false);
                return response;
            }
        }
    }

    public interface IRequireApiSession {}
}
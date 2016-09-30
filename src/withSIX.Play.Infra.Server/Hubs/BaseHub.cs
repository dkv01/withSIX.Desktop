// <copyright company="SIX Networks GmbH" file="BaseHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using MediatR;
using Microsoft.AspNet.SignalR;

namespace withSIX.Play.Infra.Server.Hubs
{

    public abstract class BaseHub : Hub, IInfrastructureService
    {
        protected IMediator _mediator { get; }

        protected BaseHub(IMediator mediator) {
            _mediator = mediator;
        }
    }
}
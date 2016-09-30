// <copyright company="SIX Networks GmbH" file="CollectionsHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;

namespace withSIX.Play.Infra.Server.Hubs
{
    public class CollectionsHub : BaseHub
    {
        public CollectionsHub(IMediator mediator) : base(mediator) {}

        public override Task OnConnected() => base.OnConnected();

        public Task<ICollection<dynamic>> GetClientCollections() => null;

        public void GetTest() {
            Clients.Caller.send("Hello");
        }
    }
}
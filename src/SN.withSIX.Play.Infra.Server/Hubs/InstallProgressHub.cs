// <copyright company="SIX Networks GmbH" file="InstallProgressHub.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShortBus;


namespace SN.withSIX.Play.Infra.Server.Hubs
{

    public class InstallProgressHub : BaseHub
    {
        public InstallProgressHub(IMediator mediator) : base(mediator) {}

        public Task<ICollection<dynamic>> GetStatusModels() {
            throw new NotImplementedException();
        }
    }
}
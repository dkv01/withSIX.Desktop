// <copyright company="SIX Networks GmbH" file="ServersAdded.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using withSIX.Play.Core.Games.Entities;

namespace withSIX.Play.Core.Games.Legacy.Events
{
    public class ServersAdded : EventArgs
    {
        public ServersAdded(Server[] servers) {
            Contract.Requires<ArgumentNullException>(servers != null);

            Servers = servers;
        }

        public Server[] Servers { get; protected set; }
    }
}
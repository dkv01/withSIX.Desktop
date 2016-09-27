// <copyright company="SIX Networks GmbH" file="CancellationTokenMapping.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;

namespace SN.withSIX.Mini.Infra.Api.Hubs
{
    public class CancellationTokenMapping
    {
        private readonly Dictionary<Guid, CancellationTokenSource> Cancellation =
            new Dictionary<Guid, CancellationTokenSource>();

        public CancellationToken AddToken(Guid id) {
            lock (Cancellation) {
                var cts = new CancellationTokenSource();
                Cancellation.Add(id, cts);
                return cts.Token;
            }
        }

        public void Cancel(Guid id) {
            lock (Cancellation)
                Cancellation[id].Cancel();
        }

        public void Remove(Guid id) {
            lock (Cancellation)
                Cancellation.Remove(id);
        }
    }
}
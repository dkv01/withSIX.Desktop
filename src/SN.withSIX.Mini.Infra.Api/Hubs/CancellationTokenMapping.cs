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
        private readonly Dictionary<Guid, CancellationTokenSource> _cancellation =
            new Dictionary<Guid, CancellationTokenSource>();

        public CancellationToken AddToken(Guid id) {
            lock (_cancellation) {
                var cts = new CancellationTokenSource();
                _cancellation.Add(id, cts);
                return cts.Token;
            }
        }

        public void Cancel(Guid id) {
            lock (_cancellation)
                _cancellation[id].Cancel();
        }

        public void Remove(Guid id) {
            lock (_cancellation)
                _cancellation.Remove(id);
        }
    }
}
// <copyright company="SIX Networks GmbH" file="ServerQueryOverallState.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace GameServerQuery
{
    class ServerQueryOverallState : IServerQueryOverallState
    {
        readonly object _lock = new object();
        public double Progress { get; set; }
        public double Maximum { get; set; }
        public bool Active { get; set; }
        public bool IsIndeterminate { get; set; }
        public int Canceled { get; set; }

        public void IncrementProcessed() {
            lock (_lock)
                Progress++;
        }

        public void IncrementCancelled() {
            lock (_lock) {
                Canceled++;
                Progress++;
            }
        }

        public int UnProcessed { get; set; }
    }
}
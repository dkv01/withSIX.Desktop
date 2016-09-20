// <copyright company="SIX Networks GmbH" file="IServerQueryOverallState.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

namespace GameServerQuery
{
    public interface IServerQueryOverallState : IProgressState
    {
        int Canceled { get; set; }
        int UnProcessed { get; set; }
        void IncrementProcessed();
        void IncrementCancelled();
    }
}
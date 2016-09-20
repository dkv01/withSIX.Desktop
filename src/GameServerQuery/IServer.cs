// <copyright company="SIX Networks GmbH" file="IServer.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Net;

namespace GameServerQuery
{
    public interface IServer
    {
        IPEndPoint Address { get; }
        void UpdateStatus(Status status);
        void UpdateInfoFromResult(ServerQueryResult result);
    }

    public enum Status
    {
        Initial,
        Success,
        SuccessParsing,
        Failure,
        FailureParsing,
        Cancelled,
        InProgress,
        Parsing,
        Processing,
        FailureProcessing
    }
}
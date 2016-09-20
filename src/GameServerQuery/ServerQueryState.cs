// <copyright company="SIX Networks GmbH" file="ServerQueryState.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace GameServerQuery
{
    public class ServerQueryState : IDisposable
    {
        private readonly IServerQueryParser _parser;
        public const long MagicPingValue = 9999;
        private readonly object _closedLock = new object();
        private readonly Stopwatch _stopWatch = new Stopwatch();
        private volatile bool _closed;

        public ServerQueryState(IServer server, IServerQueryParser parser, IPEndPoint ep = null) {
            Server = server;
            _parser = parser;
            MaxPackets = 7;
            EndPoint = ep ?? new IPEndPoint(IPAddress.Any, 0);
            ReceivedPackets = new Dictionary<int, byte[]>();
            Pings = new List<long>();
        }

        public bool HandlePlayers { get; set; }

        public IPEndPoint EndPoint { get; private set; }
        public UdpClient Client { get; set; }
        public Exception Exception { get; set; }
        public List<long> Pings { get; }
        public int MaxPackets { get; set; }
        public Dictionary<int, byte[]> ReceivedPackets { get; private set; }
        public ServerQueryResult Result { get; private set; }
        public IServer Server { get; }
        public Status Status { get; private set; }

        public void Dispose() {
            ReceivedPackets = null;
        }

        public void UpdateStatus(Status status) {
            Status = status;
            Server.UpdateStatus(status);
        }

        private void ProcessParser(IServerQueryParser parser) {
            if (Pings.Count == 0)
                Pings.Add(MagicPingValue);
            UpdateStatus(Status.Parsing);
            try {
                Result = parser.ParsePackets(this);
            } catch {
                UpdateStatus(Status.FailureParsing);
                throw;
            }
            UpdateStatus(Status.SuccessParsing);
        }

        public virtual void StartSW() {
            _stopWatch.Start();
        }

        public virtual void StopSW() {
            _stopWatch.Stop();
            if (_stopWatch.ElapsedMilliseconds > 0)
                Pings.Add(_stopWatch.ElapsedMilliseconds);
            _stopWatch.Reset();
        }

        public virtual bool IsRunningSW() {
            return _stopWatch.IsRunning;
        }

        public virtual bool TryClose() {
            if (Client == null)
                return true;
            lock (_closedLock) {
                if (_closed)
                    return true;
                _closed = true;
            }

            try {
                Client.Dispose();
                return true;
            } catch (Exception e) {}

            return false;
        }

        public void UpdateServer() {
            try {
                ProcessParser(_parser);
                UpdateStatus(Status.Processing);
                Server.UpdateInfoFromResult(Result);
            } catch {
                UpdateStatus(Status.FailureProcessing);
                throw;
            } finally {
                Result = null;
            }
            UpdateStatus(Status.Success);
        }
    }
}
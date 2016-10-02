// <copyright company="SIX Networks GmbH" file="Reactive.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameServerQuery.Parsers;

namespace GameServerQuery
{
    // TODO: Scheduler?
    // TODO: Exception handling
    // TODO: Pings
    public class ReactiveSource
    {
        private readonly SourceQueryParser _parser = new SourceQueryParser();
        public IObservable<ServerQueryResult> ProcessResults(IObservable<Result> results) => results.Select(Parse);

        ServerQueryResult Parse(Result r) => _parser.ParsePackets(r.ReceivedPackets, new List<int>());

        public IObservable<Result> GetResults(IEnumerable<IPEndPoint> eps, UdpClient socket)
            => GetResults(eps.ToObservable(), socket);

        public IObservable<Result> GetResults(IObservable<IPEndPoint> epsObs, UdpClient socket) {
            var eps2 = epsObs.Select(x => new EpState(x));

            var mapping = new ConcurrentDictionary<IPEndPoint, EpState>();
            var obs = Observable.Create<Result>(o => {
                var dsp = new CompositeDisposable();
                var receiver = CreateListener(socket)
                    .Where(x => mapping.ContainsKey(x.RemoteEndPoint))
                    .SelectMany(async x => {
                        var s = mapping[x.RemoteEndPoint];
                        var r = s.Tick(x.Buffer);
                        if (r != null)
                            await socket.SendAsync(r, r.Length, x.RemoteEndPoint).ConfigureAwait(false);
                        if (s.State == EpStateState.Complete) {
                            o.OnNext(new Result(s.Endpoint, s.ReceivedPackets));
                            //mapping.Remove(s.Endpoint);
                            s.Dispose();
                        }
                        return Unit.Default;
                    });
                dsp.Add(receiver.Subscribe());
                var sender = eps2
                    .Do(x => mapping.TryAdd(x.Endpoint, x))
                    .Select(x => Observable.FromAsync(async () => {
                        var p = x.Tick(null);
                        await socket.SendAsync(p, p.Length, x.Endpoint).ConfigureAwait(false);
                        return Unit.Default;
                    }))
                    .Merge(16);
                dsp.Add(sender.Subscribe());
                return dsp;
            });
            // http://stackoverflow.com/questions/12270642/reactive-extension-onnext
            return obs.Synchronize(); // Or use lock on the onNext call..
        }

        private static IObservable<UdpReceiveResult> CreateListener(UdpClient socket) =>
            Observable.Create<UdpReceiveResult>(obs => {
                var cts = new CancellationTokenSource();
                Task.Run(async () => {
                    try {
                        while (!cts.IsCancellationRequested)
                            obs.OnNext(await socket.ReceiveAsync().ConfigureAwait(false));
                    } catch (Exception ex) {
                        obs.OnError(ex);
                        throw;
                    }
                    obs.OnCompleted();
                }, cts.Token);
                return () => {
                    cts.Cancel();
                    cts.Dispose();
                };
            });
    }

    public struct ProcessedResult
    {
        public ProcessedResult(IPEndPoint endpoint, Dictionary<string, string> settings) {
            Endpoint = endpoint;
            Settings = settings;
        }
        public IPEndPoint Endpoint { get; }
        public Dictionary<string, string> Settings { get; }
    }

    class MultiStatus
    {
        public Dictionary<int, byte[][]> BzipDict { get; } = new Dictionary<int, byte[][]>(10);
        public Dictionary<int, byte[][]> PlainDict { get; } = new Dictionary<int, byte[][]>(10);
    }

    public struct Result
    {
        public Result(IPEndPoint endpoint, IReadOnlyList<byte[]> receivedPackets) {
            Endpoint = endpoint;
            ReceivedPackets = receivedPackets;
        }
        public IPEndPoint Endpoint { get; }
        public IReadOnlyList<byte[]> ReceivedPackets { get; }
    }

    internal class EpState
    {
        private static readonly byte[] a2SPlayerRequestH = {0xFF, 0xFF, 0xFF, 0xFF, 0x55};
        private static readonly byte[] a2SRulesRequestH = {0xFF, 0xFF, 0xFF, 0xFF, 0x56};
        private static readonly byte[] a2SInfoRequest = {
            0xFF, 0xFF, 0xFF, 0xFF, 0x54, 0x53, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20,
            0x45, 0x6E, 0x67, 0x69, 0x6E, 0x65, 0x20, 0x51, 0x75, 0x65, 0x72, 0x79, 0x00
        };
        private readonly byte[] emptyChallenge = {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};

        private MultiStatus _multiStatus;

        public EpState(IPEndPoint ep) {
            Endpoint = ep;
        }

        public IPEndPoint Endpoint { get; }
        public EpStateState State { get; private set; }

        public List<byte[]> ReceivedPackets { get; private set; } = new List<byte[]>();

        public byte[] Tick(byte[] response) {
            switch (State) {
            case EpStateState.Start:
                return a2SInfoRequest;
            case EpStateState.InfoChallenge: {
                if (response[4] != 0x49)
                    throw new Exception(string.Format("Wrong packet tag: 0x{0:X} Expected: 0x49.", response[4]));
                _multiStatus = new MultiStatus();
                var r = ProcessPacketHeader(response);
                return r != null ? TransitionToRulesChallengeState(r) : null;
            }
            case EpStateState.ReceivingInfo: {
                var r = ProcessPacketHeader(response);
                return r != null ? TransitionToRulesChallengeState(r) : null;
            }
            case EpStateState.RulesChallenge: {
                _multiStatus = new MultiStatus();
                switch (response[4]) {
                case 0x41: //challenge
                    State = EpStateState.ReceivingRules;
                    return a2SRulesRequestH.Concat(response.Skip(5)).ToArray();
                case 0x45: //no challenge needed info
                    return TransitionToPlayerChallengeState(response);
                default:
                    throw new Exception(string.Format("Wrong packet tag: 0x{0:X} Expected: 0x41 or 0x45.", response[4]));
                }
            }
            case EpStateState.ReceivingRules: {
                var r = ProcessPacketHeader(response);
                return r != null ? TransitionToPlayerChallengeState(r) : null;
            }
            case EpStateState.PlayerChallenge: {
                _multiStatus = new MultiStatus();
                switch (response[4]) {
                case 0x41: //challenge
                    State = EpStateState.ReceivingRules;
                    return a2SPlayerRequestH.Concat(response.Skip(5)).ToArray();
                case 0x44: //no challenge needed info
                    TransitionToCompleteState(response);
                    return null;
                default:
                    throw new Exception(string.Format("Wrong packet tag: 0x{0:X} Expected: 0x41 or 0x45.", response[4]));
                }
            }
            case EpStateState.ReceivingPlayers: {
                var r = ProcessPacketHeader(response);
                if (r != null)
                    TransitionToCompleteState(r);
                return null;
            }
            default: {
                throw new Exception("Is in invalid state");
            }
            }
        }

        private byte[] TransitionToRulesChallengeState(byte[] r) {
            ReceivedPackets[0] = r;
            State = EpStateState.RulesChallenge;
            emptyChallenge[4] = 0x56;
            return emptyChallenge;
        }

        private byte[] TransitionToPlayerChallengeState(byte[] r) {
            ReceivedPackets[1] = r;
            State = EpStateState.PlayerChallenge;
            emptyChallenge[4] = 0x55;
            return emptyChallenge;
        }

        private void TransitionToCompleteState(byte[] r) {
            ReceivedPackets[2] = r;
            State = EpStateState.Complete;
            _multiStatus = null;
        }

        public void Dispose() {
            ReceivedPackets = null;
        }

        private byte[] ProcessPacketHeader(byte[] reply) {
            Contract.Requires<ArgumentNullException>(reply != null);

            var pos = 0;
            byte[] header = {0xFF, 0xFF, 0xFF};
            var match = header.All(b => reply[pos++] == b);
            if (!match)
                return null;

            if (reply[pos] == 0xFF)
                return reply;
            if (reply[pos] == 0xFE)
                return CollectPacket(reply, ++pos);
            return null;
        }

        private byte[] CollectPacket(byte[] reply, int pos) {
            //TODO: test if this works on old source server or mockup
            var id = BitConverter.ToInt32(reply, pos);
            pos += 4;
            var bzipped = (id & 0x80000000) != 0;
            var total = reply[pos++];
            var number = reply[pos++];
            pos += 2; //ignoring next short
            if (bzipped) {
                throw new Exception("bzip2 compression not implemented");
                /*
                if (!status.BzipDict.ContainsKey(id))
                {
                    status.BzipDict[id] = new byte[total + 2][];
                    status.BzipDict[id][total] = new byte[1];
                }
                if (number == 0)
                {
                    status.BzipDict[id][total + 1] = reply.Skip(pos).Take(4).ToArray();
                    pos += 4;
                }
                status.BzipDict[id][number] = reply.Skip(pos).ToArray();
                var runTotal = status.BzipDict[id][total][0]++;
                if (runTotal == total) JoinPackets(id, total, true);
                 */
            }
            //else
            if (!_multiStatus.PlainDict.ContainsKey(id)) {
                _multiStatus.PlainDict[id] = new byte[total + 1][];
                _multiStatus.PlainDict[id][total] = new byte[1];
            }
            _multiStatus.PlainDict[id][number] = reply.Skip(pos).ToArray();
            var runTotal = _multiStatus.PlainDict[id][total][0]++;
            if (runTotal == total)
                return JoinPackets(id, total, false);

            return null;
        }

        private byte[] JoinPackets(int id, byte total, bool bzipped) {
            var multiPkt = bzipped ? _multiStatus.BzipDict[id] : _multiStatus.PlainDict[id];
            var len = multiPkt.Sum(pkt => pkt.Length);
            for (var i = total; i < multiPkt.Length; i++)
                len -= multiPkt[i].Length;
            var replyPkt = new byte[len];
            var pos = 0;
            for (var i = 0; i < total; i++) {
                Buffer.BlockCopy(multiPkt[i], 0, replyPkt, pos, multiPkt[i].Length);
                pos += multiPkt[i].Length;
            }
            if (bzipped) {
                //var input = new MemoryStream(replyPkt);
                //var output = new MemoryStream();
                //BZip2.Decompress(input, output, true);
                //replyPkt = output.ToArray();
            }
            var valid = true;
            if (total + 1 < multiPkt.Length) {
                //var size = BitConverter.ToInt32(multiPkt[total + 1], 0);
                //var crc = BitConverter.ToInt32(multiPkt[total + 1], 4);
                //if (replyPkt.Length * 8 != size) valid = false;
                //var checkCrc = new Crc32();
                //checkCrc.Update(replyPkt);
                //if (checkCrc.Value != crc) valid = false;
            }

            if (!valid) {
                throw new Exception("split packet not decompressed properly");
                //TODO: check if at least header is intact so query can be retried
            }
            return replyPkt;
        }
    }

    public enum EpStateState
    {
        Start,
        InfoChallenge,
        ReceivingInfo,
        ReceivingRules,
        ReceivingPlayers,
        RulesChallenge,
        PlayerChallenge,
        Complete
    }
}
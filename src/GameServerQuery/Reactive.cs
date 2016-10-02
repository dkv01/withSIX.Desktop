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
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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

        public IObservable<ServerQueryResult> ProcessResults(IObservable<Result> results) => results.Select(x => {
            try {
                return Parse(x);
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
            return null;
        }).Where(x => x != null);

        ServerQueryResult Parse(Result r) => _parser.ParsePackets(r.Endpoint, r.ReceivedPackets, new List<int>());

        public IObservable<Result> GetResults(IEnumerable<IPEndPoint> eps, UdpClient socket)
            => GetResults(eps.ToObservable(), socket);

        public IObservable<Result> GetResults(IObservable<IPEndPoint> epsObs, UdpClient socket) {
            var eps2 = epsObs.Select(x => new EpState(x));

            var heartbeat = Subject.Synchronize(new Subject<Unit>());

            var scheduler = new EventLoopScheduler();

            var count = 0;
            var mapping = new Dictionary<IPEndPoint, EpState>();
            var obs = Observable.Create<Result>(o => {
                var dsp = new CompositeDisposable();
                var receiver = CreateListener(socket)
                    .Where(x => mapping.ContainsKey(x.RemoteEndPoint))
                    .Select(x => Observable.FromAsync(async () => {
                        try {
                            heartbeat.OnNext(Unit.Default);
                            var s = mapping[x.RemoteEndPoint];
                            Console.WriteLine("ReceivedPackets: " + count++);
                            var r = s.Tick(x.Buffer);
                            if (r != null)
                                await socket.SendAsync(r, r.Length, x.RemoteEndPoint).ConfigureAwait(false);
                            if (s.State == EpStateState.Complete) {
                                o.OnNext(new Result(s.Endpoint, s.ReceivedPackets.Values.ToArray()));
                                //mapping.Remove(s.Endpoint);
                                s.Dispose();
                            }
                        } catch (Exception ex) {
                            Console.WriteLine(ex);
                        }
                        return Unit.Default;
                    }, scheduler)).Merge(1);
                dsp.Add(heartbeat.Throttle(TimeSpan.FromSeconds(5))
                    .Subscribe(_ => {
                        Console.WriteLine($"" +
                                          $"Stats: Still in Start: {mapping.Values.Count(x => x.State == EpStateState.Start)}, Completed: {mapping.Values.Count(x => x.State == EpStateState.Complete)}  " +
                                          $"total count: {mapping.Values.Count}" +
                                          $"{string.Join("\n", mapping.Values.GroupBy(x => x.State).OrderByDescending(x => x.Count()).Select(x => x.Key + " " + x.Count()))}");
                        o.OnCompleted();
                    }));
                dsp.Add(receiver.Subscribe());

                var sender = eps2
                    .Do(x => {
                        lock (mapping) {
                            mapping.Add(x.Endpoint, x);
                        }
                    })
                    .Select(x => Observable.FromAsync(async () => {
                        // TODO: We could also make an observable sequence out of the state transition of each element
                        // have each element timeout after e.g 5 seconds of non-activity
                        // and then maybe have a degreeOfParallelism mixed in..
                        // oh and retryability hm :D
                        try {
                            var p = x.Tick(null);
                            await socket.SendAsync(p, p.Length, x.Endpoint).ConfigureAwait(false);
                            heartbeat.OnNext(Unit.Default);
                        } catch (Exception ex) {
                            Console.WriteLine(ex);
                        }
                        return Unit.Default;
                    }, scheduler)
                    .Delay(TimeSpan.FromMilliseconds(25), scheduler))
                    .Merge(1);
                dsp.Add(sender.Subscribe());
                // TODO
                return dsp;
            });
            // http://stackoverflow.com/questions/12270642/reactive-extension-onnext
            return obs.Synchronize(scheduler); // Or use lock on the onNext call..
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

        public Dictionary<int, byte[]> ReceivedPackets { get; private set; } = new Dictionary<int, byte[]>();

        public bool InclPlayers { get; set; }

        public byte[] Tick(byte[] response) {
            switch (State) {
            case EpStateState.Start:
                return TransitionToInfoChallenge();
            case EpStateState.InfoChallenge: {
                _multiStatus = new MultiStatus();
                var r = ProcessPacketHeader(response);
                return r == null ? TransitionToReceivingInfoState() : TransitionToRulesChallengeState(r);
            }
            case EpStateState.ReceivingInfo: {
                var r = ProcessPacketHeader(response);
                return r == null ? null : TransitionToRulesChallengeState(r);
            }
            case EpStateState.RulesChallenge: {
                _multiStatus = new MultiStatus();
                switch (response[4]) {
                case 0x41: //challenge
                    State = EpStateState.ReceivingRules;
                    return a2SRulesRequestH.Concat(response.Skip(5)).ToArray();
                case 0x45: //no challenge needed info
                    return InclPlayers ? TransitionToPlayerChallengeState(response) : TransitionToCompleteStateFromRules(response);
                default:
                    throw new Exception(string.Format("Wrong packet tag: 0x{0:X} Expected: 0x41 or 0x45.", response[4]));
                }
            }
            case EpStateState.ReceivingRules: {
                var r = ProcessPacketHeader(response);
                return r != null ? (InclPlayers ? TransitionToPlayerChallengeState(response) : TransitionToCompleteStateFromRules(response)) : null;
            }
            case EpStateState.PlayerChallenge: {
                _multiStatus = new MultiStatus();
                switch (response[4]) {
                case 0x41: //challenge
                    State = EpStateState.ReceivingPlayers;
                    return a2SPlayerRequestH.Concat(response.Skip(5)).ToArray();
                case 0x44: //no challenge needed info
                    return TransitionToCompleteStateFromPlayers(response);
                default:
                    throw new Exception(string.Format("Wrong packet tag: 0x{0:X} Expected: 0x41 or 0x45.", response[4]));
                }
            }
            case EpStateState.ReceivingPlayers: {
                var r = ProcessPacketHeader(response);
                return r != null ? TransitionToCompleteStateFromPlayers(r) : null;
            }
            default: {
                throw new Exception("Is in invalid state");
            }
            }
        }

        private byte[] TransitionToReceivingInfoState() {
            State = EpStateState.ReceivingInfo;
            return null;
        }

        private byte[] TransitionToInfoChallenge() {
            State = EpStateState.InfoChallenge;
            return a2SInfoRequest;
        }

        private byte[] TransitionToRulesChallengeState(byte[] r) {
            if (r[4] != 0x49)
                throw new Exception(string.Format("Wrong packet tag: 0x{0:X} Expected: 0x49.", r[4]));
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

        private byte[] TransitionToCompleteStateFromRules(byte[] r) {
            ReceivedPackets[1] = r;
            State = EpStateState.Complete;
            _multiStatus = null;
            return null;
        }

        private byte[] TransitionToCompleteStateFromPlayers(byte[] r) {
            ReceivedPackets[2] = r;
            State = EpStateState.Complete;
            _multiStatus = null;
            return null;
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
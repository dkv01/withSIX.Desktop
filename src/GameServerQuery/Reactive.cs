// <copyright company="SIX Networks GmbH" file="Reactive.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
    // TODO: Exception handling
    // TODO: Pings
    // TODO: Retry
    public class ReactiveSource
    {
        private readonly SourceQueryParser _parser = new SourceQueryParser();

        public IObservable<ServerQueryResult> ProcessResults(IObservable<IResult> results) => results
            .Where(x => x.Success)
            .Select(x => {
                try {
                    return Parse(x);
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                }
                return null;
            }).Where(x => x != null);

        ServerQueryResult Parse(IResult r) => _parser.ParsePackets(r.Endpoint, r.ReceivedPackets, new List<int>());

        public IObservable<IResult> GetResults(IEnumerable<IPEndPoint> eps, UdpClient socket, int degreeOfParallelism = 50)
            => GetResults(eps.ToObservable(), socket, degreeOfParallelism);

        public IObservable<IResult> GetResults(IObservable<IPEndPoint> epsObs, UdpClient socket, int degreeOfParallelism = 50) {
            var mapping = new ConcurrentBag<EpState>();
            var scheduler = new EventLoopScheduler();
            var obs = Observable.Create<IResult>(o => {
                var count = 0;
                var count2 = 0;
                var count3 = 0;

                var dsp = new CompositeDisposable();
                var listener = CreateListener(socket)
                    .ObserveOn(scheduler)
                    .Publish();

                dsp.Add(listener.Connect());

                var sender = epsObs
                    .ObserveOn(scheduler)
                    .Select(x => new EpState2(x, listener.Where(r => r.RemoteEndPoint.Equals(x)).Select(r => r.Buffer),
                        d => socket.SendAsync(d, d.Length, x)))
                    .Do(x => mapping.Add(x))
                    .Select(x => Observable.Create<IResult>(io => {
                        Console.WriteLine($"Sending initial packet: {Interlocked.Increment(ref count)}");
                        return x.Results.Subscribe(io.OnNext, io.OnError, io.OnCompleted);
                    }))
                    .ObserveOn(scheduler)
                    .Merge(degreeOfParallelism) // TODO: Instead try to limit sends at a time? hm
                    .Do(
                        x => {
                            Console.WriteLine($"Finished Processing: {Interlocked.Increment(ref count3)}. {x.Success} {x.Ping}ms");
                        });
                var sub = sender
                    .Subscribe(o.OnNext, o.OnError, () => {
                        Console.WriteLine($"" +
                                          $"Stats: total count: {mapping.Count}\n" +
                                          $"{string.Join("\n", mapping.GroupBy(x => x.State).OrderByDescending(x => x.Count()).Select(x => x.Key + " " + x.Count()))}");
                        o.OnCompleted();
                    });
                dsp.Add(sub);
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
                        return;
                    }
                    obs.OnCompleted();
                }, cts.Token);
                return () => {
                    cts.Cancel();
                    cts.Dispose();
                };
            });
    }

    class EpState2 : EpState
    {
        private readonly IObservable<byte[]> _receiver;
        private readonly Func<byte[], Task> _sender;
        readonly Stopwatch _sw = new Stopwatch();
        readonly List<long> _pings = new List<long>();

        public EpState2(IPEndPoint ep, IObservable<byte[]> receiver, Func<byte[], Task> sender) : base(ep) {
            _receiver = receiver;
            _sender = sender;
        }

        public IObservable<IResult> Results => Observable.Create<IResult>(async obs => {
            var signal = new Subject<Unit>();
            var l = _receiver
                .Select(x => Observable.FromAsync(() => Process(x)))
                //.Do(x => Console.WriteLine($"Receiving Package for {Endpoint}"))
                .Do(_ => signal.OnNext(Unit.Default))
                .Merge(1)
                .Select(_ => State)
                .Where(x => (x == EpStateState.Complete) || (x == EpStateState.Timeout))
                .Merge(signal.Throttle(TimeSpan.FromSeconds(5)).Select(x => EpStateState.Timeout))
                .Take(1);
            //.Do(x => Console.WriteLine($"Finished Processing for {Endpoint}: {x}"));
            var dsp = l.Subscribe(s => obs.OnNext(s == EpStateState.Complete
                ? (IResult) new Result(Endpoint, ReceivedPackets.Values.ToArray(), (long) _pings.Average())
                : new FailedResult(Endpoint)), obs.OnError, obs.OnCompleted);
            var data = Tick(null);
            await Send(data).ConfigureAwait(false);
            signal.OnNext(Unit.Default); // kick off the hearbeat, incase we don't get anything back..
            return () => {
                _sw.Stop();
                dsp.Dispose();
            };
        });

        Task Send(byte[] data) {
            _sw.Start();
            return _sender(data);
        }

        async Task Process(byte[] arg) {
            var s = TryTick(arg);
            if (s != null)
                await Send(s).ConfigureAwait(false);
        }

        private byte[] TryTick(byte[] buffer) {
            _sw.Stop();
            _pings.Add(_sw.ElapsedMilliseconds);
            _sw.Reset();
            try {
                return Tick(buffer);
            } catch (Exception ex) {
                Console.WriteLine(ex);
                return null;
            }
        }
    }

    public static class ReactiveExtensions
    {
        public static IObservable<ServerPageArgs> GetParsedServersObservable(this SourceMasterQuery This,
            CancellationToken cancelToken,
            bool forceLocal = false, int limit = 0) => Observable.Create<ServerPageArgs>(async obs => {
            try {
                using (BuildObservable(This).Subscribe(obs.OnNext)) {
                    await This.RetrieveAsync(cancelToken, limit).ConfigureAwait(false);
                }
            } catch (Exception ex) {
                obs.OnError(ex);
                return;
            }
            obs.OnCompleted();
        });

        private static IObservable<ServerPageArgs> BuildObservable(SourceMasterQuery master)
            => Observable.FromEvent<EventHandler<ServerPageArgs>, ServerPageArgs>(handler => {
                    EventHandler<ServerPageArgs> evtHandler = (sender, e) => handler(e);
                    return evtHandler;
                },
                evtHandler => master.ServerPageReceived += evtHandler,
                evtHandler => master.ServerPageReceived -= evtHandler);
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

    public interface IResult
    {
        IPEndPoint Endpoint { get; }
        bool Success { get; }
        IReadOnlyList<byte[]> ReceivedPackets { get; }
        long Ping { get; }
    }

    public struct Result : IResult
    {
        public Result(IPEndPoint endpoint, IReadOnlyList<byte[]> receivedPackets, long ping) {
            Endpoint = endpoint;
            ReceivedPackets = receivedPackets;
            Ping = ping;
        }

        public bool Success => true;
        public IPEndPoint Endpoint { get; }
        public IReadOnlyList<byte[]> ReceivedPackets { get; }
        public long Ping { get; }
    }

    public struct FailedResult : IResult
    {
        public IPEndPoint Endpoint { get; }
        public IReadOnlyList<byte[]> ReceivedPackets { get; }
        public bool Success => false;
        public long Ping => -1;

        public FailedResult(IPEndPoint endpoint) {
            Endpoint = endpoint;
            ReceivedPackets = new List<byte[]>();
        }
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
                    return InclPlayers
                        ? TransitionToPlayerChallengeState(response)
                        : TransitionToCompleteStateFromRules(response);
                default:
                    throw new Exception(string.Format("Wrong packet tag: 0x{0:X} Expected: 0x41 or 0x45.", response[4]));
                }
            }
            case EpStateState.ReceivingRules: {
                var r = ProcessPacketHeader(response);
                return r != null
                    ? (InclPlayers
                        ? TransitionToPlayerChallengeState(response)
                        : TransitionToCompleteStateFromRules(response))
                    : null;
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
        Complete,
        Timeout
    }
}
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameServerQuery.Parsers;

namespace GameServerQuery
{
    // TODO: Exception handling
    // TODO: Pings
    // TODO: Retry

    public class QuerySettings
    {
        public bool InclRules { get; set; }
        public bool InclPlayers { get; set; }
        public int DegreeOfParallelism { get; set; } = 50;
    }

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

        ServerQueryResult Parse(IResult r) => _parser.ParsePackets(r);

        public UdpClient CreateUdpClient() {
            var udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            udpClient.Client.ReceiveBufferSize = 25*1024*1024;
            //udpClient.Ttl = 255;
            // http://stackoverflow.com/questions/7201862/an-existing-connection-was-forcibly-closed-by-the-remote-host
            var IOC_IN = 0x80000000;
            uint IOC_VENDOR = 0x18000000;
            var SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
            udpClient.Client.IOControl((int) SIO_UDP_CONNRESET, new[] {Convert.ToByte(false)}, null);
            return udpClient;
        }

        public IObservable<IResult> GetResults(IEnumerable<IPEndPoint> eps, UdpClient socket,
                QuerySettings settings = null)
            => GetResults(eps.ToObservable(), socket, settings);

        public IObservable<IResult> GetResults(IObservable<IPEndPoint> epsObs, UdpClient socket,
            QuerySettings settings) {
            if (settings == null)
                settings = new QuerySettings();
            var mapping = new ConcurrentBag<EpState>();
            var obs = Observable.Create<IResult>(o => {
                var count = 0;
                var count2 = 0;
                var count3 = 0;

                var dsp = new CompositeDisposable();
                //var scheduler = new EventLoopScheduler();
                //dsp.Add(scheduler);
                var listener = CreateListener(socket)
                    .Publish();

                dsp.Add(listener.Connect());

                var sender = epsObs
                    //.ObserveOn(scheduler)
                    .Select(x => new EpState2(x, settings,
                        listener.Where(r => r.RemoteEndPoint.Equals(x))
                            .Select(r => r.Buffer)
                            .ObserveOn(TaskPoolScheduler.Default),
                        d => socket.SendAsync(d, d.Length, x)))
                    .Do(x => mapping.Add(x))
                    .Select(x => x.Results)
                    //.Select(x => Intercept(_ => Console.WriteLine($"Sending initial packet: {Interlocked.Increment(ref count)}"), x))
                    //.ObserveOn(scheduler)
                    .Merge(settings.DegreeOfParallelism); // TODO: Instead try to limit sends at a time? hm
                //.Do(x => Console.WriteLine($"Finished Processing: {Interlocked.Increment(ref count3)}. {x.Success} {x.Ping}ms"));
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
            return obs.Synchronize(); // Or use lock on the onNext call..
        }

        private static IObservable<T> Intercept<T>(Action<IObservable<T>> act, IObservable<T> x)
            => Observable.Create<T>(io => {
                act(x);
                return x.Subscribe(io.OnNext, io.OnError, io.OnCompleted);
            });

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
        readonly List<long> _pings = new List<long>();
        private readonly IObservable<byte[]> _receiver;
        private readonly Func<byte[], Task> _sender;
        readonly Stopwatch _sw = new Stopwatch();

        public EpState2(IPEndPoint ep, QuerySettings settings, IObservable<byte[]> receiver, Func<byte[], Task> sender)
            : base(ep, settings) {
            _receiver = receiver;
            _sender = sender;
        }

        public IObservable<IResult> Results => Observable.Create<IResult>(async obs => {
            var signal = new Subject<Unit>();
            var l = _receiver
                .Do(_ => signal.OnNext(Unit.Default))
                .Select(x => Observable.FromAsync(() => Process(x)))
                //.Do(x => Console.WriteLine($"Receiving Package for {Endpoint}"))
                .Merge(1)
                .Select(_ => State)
                .Where(x => (x == EpStateState.Complete) || (x == EpStateState.Timeout))
                .Merge(signal.Throttle(TimeSpan.FromSeconds(5)).Select(x => EpStateState.Timeout))
                .Take(1);
            //.Do(x => Console.WriteLine($"Finished Processing for {Endpoint}: {x}"));
            var dsp = l.Subscribe(s => obs.OnNext(s == EpStateState.Complete
                ? (IResult) new Result(Endpoint, ReceivedPackets, Convert.ToInt32(_pings.Average()))
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
                using (This.BuildObservable().Subscribe(obs.OnNext)) {
                    await This.GetParsedServers(cancelToken, limit).ConfigureAwait(false);
                }
            } catch (Exception ex) {
                obs.OnError(ex);
                return;
            }
            obs.OnCompleted();
        });

        public static IObservable<ServerPageArgs> BuildObservable(this SourceMasterQuery master)
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
        private const int SinglePacket = -1;
        private const int MultiPacket = -2;

        readonly Dictionary<int, MStatus> _bzipDict = new Dictionary<int, MStatus>();
        readonly Dictionary<int, MStatus> _plainDict = new Dictionary<int, MStatus>();

        public byte[] ProcessPacketHeader(byte[] reply) {
            Contract.Requires<ArgumentNullException>(reply != null);

            var reader = new ByteArrayReader(reply);
            var header = reader.ReadInt();
            switch (header) {
            case SinglePacket:
                return reply;
            case MultiPacket:
                return CollectPacket(reader);
            }
            throw new InvalidOperationException(
                $"Did not find the right packet header, found: {header}, expected {SinglePacket} or {MultiPacket}");
        }

        private byte[] CollectPacket(ByteArrayReader reader) {
            var id = reader.ReadInt();
            var total = reader.ReadByte();
            var number = reader.ReadByte();
            var size = reader.ReadShort();

            var bzipped = (id & 0x80000000) != 0;
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
            if (!_plainDict.ContainsKey(id))
                _plainDict[id] = new MStatus(total);
            var s = _plainDict[id];
            s.Content[number] = reader.ReadRest();
            return ++s.Received == total ? JoinPackets(id, total, bzipped) : null;
        }

        private byte[] JoinPackets(int id, int total, bool bzipped) {
            var multiPkt = bzipped ? _bzipDict[id] : _plainDict[id];
            var c = multiPkt.Content;
            var len = c.Sum(pkt => pkt.Length);
            var replyPkt = new byte[len];
            var pos = 0;
            foreach (var t in c) {
                Buffer.BlockCopy(t, 0, replyPkt, pos, t.Length);
                pos += t.Length;
            }
            if (bzipped) {
                //var input = new MemoryStream(replyPkt);
                //var output = new MemoryStream();
                //BZip2.Decompress(input, output, true);
                //replyPkt = output.ToArray();
            }
            /*
            if (total < c.Length) {
                var valid = true;
                var size = BitConverter.ToInt32(multiPkt[total + 1], 0);
                var crc = BitConverter.ToInt32(multiPkt[total + 1], 4);
                if (replyPkt.Length * 8 != size) valid = false;
                var checkCrc = new Crc32();
                checkCrc.Update(replyPkt);
                if (checkCrc.Value != crc) valid = false;
                if (!valid) {
                  throw new Exception("split packet not decompressed properly");
                // TODO: check if at least header is intact so query can be retried
                }
            }
            */

            return replyPkt;
        }
    }

    public class ByteArrayReader
    {
        private readonly byte[] _b;
        private int _pos;

        public ByteArrayReader(byte[] b) {
            _b = b;
            _pos = 0;
        }

        public int ReadInt() {
            var r = BitConverter.ToInt32(_b, _pos);
            _pos += 4;
            return r;
        }

        public int ReadShort() {
            var r = BitConverter.ToInt16(_b, _pos);
            _pos += 2;
            return r;
        }


        public long ReadLong() {
            var r = BitConverter.ToInt64(_b, _pos);
            _pos += 8;
            return r;
        }

        public byte ReadByte() => _b[_pos++];

        public int ReadAsInt() => ReadByte();

        public void Skip(int count) => _pos += count;

        public string ReadStringUntil(byte until = 0x00) {
            var start = _pos;
            while (_b[_pos++] != 0x00) {}
            return Encoding.UTF8.GetString(_b, start, _pos - start - 1);
        }


        public byte[] ReadRest() {
            var r = _b.Skip(_pos).ToArray();
            _pos = _b.Length;
            return r;
        }

        public void WhileNotOutOfBounds(int ruleCount, Action action) {
            for (var i = 0; i < ruleCount; i++) {
                if (_pos >= _b.Length)
                    break;
                action();
            }
        }
    }

    class MStatus
    {
        public MStatus(int total) {
            Content = new byte[total][];
        }

        public byte[][] Content { get; set; }
        public int Received { get; set; }
    }

    public interface IResult
    {
        IPEndPoint Endpoint { get; }
        bool Success { get; }
        IDictionary<string, byte[]> ReceivedPackets { get; }
        int Ping { get; }
    }

    public struct Result : IResult
    {
        public Result(IPEndPoint endpoint, IDictionary<string, byte[]> receivedPackets, int ping) {
            Endpoint = endpoint;
            ReceivedPackets = receivedPackets;
            Ping = ping;
        }

        public bool Success => true;
        public IPEndPoint Endpoint { get; }
        public IDictionary<string, byte[]> ReceivedPackets { get; }
        public int Ping { get; }
    }

    public struct FailedResult : IResult
    {
        public IPEndPoint Endpoint { get; }
        public IDictionary<string, byte[]> ReceivedPackets { get; }
        public bool Success => false;
        public int Ping => -1;

        public FailedResult(IPEndPoint endpoint) {
            Endpoint = endpoint;
            ReceivedPackets = new Dictionary<string, byte[]>();
        }
    }

    internal class EpState
    {
        private const byte InfoResponse = 0x49;
        private const byte ChallengeResponse = 0x41;
        private const byte PlayerResponse = 0x44;
        private const byte RulesResponse = 0x45;
        private const byte RuleRequest = 0x56;
        private const byte PlayerRequest = 0x55;
        private static readonly byte[] a2SPlayerRequestH = {0xFF, 0xFF, 0xFF, 0xFF, PlayerRequest};
        private static readonly byte[] a2SRulesRequestH = {0xFF, 0xFF, 0xFF, 0xFF, RuleRequest};
        private static readonly byte[] a2SInfoRequest = {
            0xFF, 0xFF, 0xFF, 0xFF, 0x54, 0x53, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20,
            0x45, 0x6E, 0x67, 0x69, 0x6E, 0x65, 0x20, 0x51, 0x75, 0x65, 0x72, 0x79, 0x00
        };
        // reused
        private readonly byte[] _emptyChallenge = {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
        private readonly QuerySettings _settings;

        private MultiStatus _multiStatus;

        public EpState(IPEndPoint ep, QuerySettings settings) {
            _settings = settings;
            Endpoint = ep;
        }

        public IPEndPoint Endpoint { get; }
        public EpStateState State { get; private set; }

        public Dictionary<string, byte[]> ReceivedPackets { get; private set; } = new Dictionary<string, byte[]>();

        private byte[] GetChallenge(byte b) {
            _emptyChallenge[4] = b;
            return _emptyChallenge;
        }

        // TODO: Currently we only support exact order, without exclusion: Info, Rules, Player.
        public byte[] Tick(byte[] response) {
            switch (State) {
            case EpStateState.Start:
                return TransitionToInfoChallenge();
            case EpStateState.InfoChallenge: {
                _multiStatus = new MultiStatus();
                var r = _multiStatus.ProcessPacketHeader(response);
                return r == null ? TransitionToReceivingInfoState() : HandleInfoEnd(r);
            }
            case EpStateState.ReceivingInfo: {
                var r = _multiStatus.ProcessPacketHeader(response);
                return r == null
                    ? null
                    : HandleInfoEnd(r);
            }
            case EpStateState.RulesChallenge: {
                _multiStatus = new MultiStatus();
                switch (response[4]) {
                case ChallengeResponse: //challenge
                    return TransitionToReceivingRulesState(response);
                case RulesResponse: //no challenge needed info
                    return HandleRulesEnd(response);
                default:
                    throw new Exception(string.Format("Wrong packet tag: 0x{0:X} Expected: 0x41 or 0x45.", response[4]));
                }
            }
            case EpStateState.ReceivingRules: {
                var r = _multiStatus.ProcessPacketHeader(response);
                return r != null
                    ? HandleRulesEnd(r)
                    : null;
            }
            case EpStateState.PlayerChallenge: {
                _multiStatus = new MultiStatus();
                switch (response[4]) {
                case ChallengeResponse: //challenge
                    return TransitionToReceivingPlayers(response);
                case PlayerResponse: //no challenge needed info
                    return HandlePlayersEnd(response);
                default:
                    throw new Exception(string.Format("Wrong packet tag: 0x{0:X} Expected: 0x41 or 0x45.", response[4]));
                }
            }
            case EpStateState.ReceivingPlayers: {
                var r = _multiStatus.ProcessPacketHeader(response);
                return r != null ? HandlePlayersEnd(r) : null;
            }
            default: {
                throw new Exception("Is in invalid state");
            }
            }
        }

        private byte[] HandleInfoEnd(byte[] r) {
            ConfirmTag(r, InfoResponse);
            Save(r, "settings");
            if (_settings.InclRules)
                return TransitionToRulesChallengeState();
            return _settings.InclPlayers ? TransitionToPlayerChallengeState() : TransitionToCompleteState();
        }

        private byte[] HandleRulesEnd(byte[] r) {
            Save(r, "rules");
            return _settings.InclPlayers ? TransitionToPlayerChallengeState() : TransitionToCompleteState();
        }

        private byte[] HandlePlayersEnd(byte[] r) {
            Save(r, "players");
            return TransitionToCompleteState();
        }

        private byte[] TransitionToInfoChallenge() {
            State = EpStateState.InfoChallenge;
            return a2SInfoRequest;
        }

        private byte[] TransitionToReceivingInfoState() {
            State = EpStateState.ReceivingInfo;
            return null;
        }

        private byte[] TransitionToRulesChallengeState() {
            State = EpStateState.RulesChallenge;
            return GetChallenge(RuleRequest);
        }

        private void Save(byte[] r, string type) {
            _multiStatus = null;
            ReceivedPackets[type] = r;
        }

        private byte[] TransitionToReceivingRulesState(byte[] response) {
            State = EpStateState.ReceivingRules;
            return a2SRulesRequestH.Concat(response.Skip(5)).ToArray();
        }

        private byte[] TransitionToPlayerChallengeState() {
            State = EpStateState.PlayerChallenge;
            return GetChallenge(PlayerRequest);
        }

        private byte[] TransitionToReceivingPlayers(byte[] response) {
            State = EpStateState.ReceivingPlayers;
            return a2SPlayerRequestH.Concat(response.Skip(5)).ToArray();
        }

        private byte[] TransitionToCompleteState() {
            State = EpStateState.Complete;
            return null;
        }

        private static void ConfirmTag(byte[] r, int checkTag) {
            if (r[4] != checkTag)
                throw new Exception($"Wrong packet tag: 0x{r[4]:X} Expected: 0x{checkTag:X}.");
        }

        public void Dispose() {
            ReceivedPackets = null;
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
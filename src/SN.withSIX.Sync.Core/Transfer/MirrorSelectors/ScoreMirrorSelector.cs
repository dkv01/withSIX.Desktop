// <copyright company="SIX Networks GmbH" file="ScoreMirrorSelector.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using SmartAssembly.Attributes;
using SN.withSIX.Core;
using SN.withSIX.Core.Helpers;
using SN.withSIX.Sync.Core.Legacy;

namespace SN.withSIX.Sync.Core.Transfer.MirrorSelectors
{
    public class ScoreMirrorSelector : IMirrorSelector, IDisposable
    {
        const int DefaultScoreLimit = 250;
        const int ScoreCeil = 10;
        readonly Dictionary<Uri, HostState> _hostScores;
        readonly int _scoreLimit;
        readonly TimerWithElapsedCancellationOnExceptionOnly _scoreMonitor;
        protected TimeSpan FailedScoreIncreaseEvery = TimeSpan.FromSeconds(100);

        public ScoreMirrorSelector(IHostChecker hostChecker, params Uri[] hostPool)
            : this(DefaultScoreLimit, hostChecker, (IReadOnlyCollection<Uri>) hostPool) {}

        public ScoreMirrorSelector(int scoreLimit, IHostChecker hostChecker, params Uri[] hostPool)
            : this(scoreLimit, hostChecker, (IReadOnlyCollection<Uri>) hostPool) {}

        public ScoreMirrorSelector(int scoreLimit, IHostChecker hostChecker, IReadOnlyCollection<Uri> hostPool) {
            ThrowWhenHostPoolIsNullOrEmpty(hostPool);
            _scoreLimit = scoreLimit;
            _hostScores = hostChecker.SortAndValidateHosts(hostPool).ToDictionary(x => x, x => new HostState());
            _scoreMonitor = new TimerWithElapsedCancellationOnExceptionOnly(1*1000, IncreaseScoresWhenNeeded);
        }

        public ScoreMirrorSelector(IHostChecker hostChecker, IReadOnlyCollection<Uri> hostPool)
            : this(DefaultScoreLimit, hostChecker, hostPool) {}

        public int ProgramFailures { get; private set; }

        public void Dispose() {
            Dispose(true);
        }

        public Uri GetHost() {
            ConfirmProgramFailures();
            var host = GetFirstHost();
            ConfirmHostValidity(host.Value);
            return host.Key;
        }

        public void ProgramFailure() {
            ProgramFailures++;
        }

        public void Failure(Uri host) {
            ConfirmHostExists(host);
            DecreaseScore(_hostScores[host]);
        }

        public void Success(Uri host) {
            ConfirmHostExists(host);
            IncreaseScore(_hostScores[host]);
        }

        private void ConfirmProgramFailures() {
            if (ProgramFailures > 1000)
                throw new TooManyProgramExceptions();
        }

        static void ThrowWhenHostPoolIsNullOrEmpty(IEnumerable<Uri> hostPool) {
            if (hostPool == null)
                throw new ArgumentNullException();
            if (!hostPool.Any())
                throw new EmptyHostList();
        }

        static void DecreaseScore(HostState hostState) {
            lock (hostState) {
                hostState.FailStamp = Tools.Generic.GetCurrentUtcDateTime;
                hostState.Score--;
            }
        }

        static void IncreaseScore(HostState hostState) {
            lock (hostState) {
                if (hostState.Score < ScoreCeil)
                    hostState.Score++;
            }
        }

        void IncreaseScoresWhenNeeded() {
            foreach (var state in GetStatesToIncrease())
                IncreaseScore(state);
        }

        void ConfirmHostExists(Uri host) {
            if (!_hostScores.ContainsKey(host))
                throw new NoSuchMirror();
        }

        void ConfirmHostValidity(HostState state) {
            if (state.Score <= -_scoreLimit)
                throw new HostListExhausted();
        }

        KeyValuePair<Uri, HostState> GetFirstHost() {
            try {
                return _hostScores.OrderByDescending(x => x.Value.Score).First();
            } catch (InvalidOperationException e) {
                throw new NoHostsAvailableException("perhaps reconfigure preferred protocol settings", e);
            }
        }

        IEnumerable<HostState> GetStatesToIncrease() => _hostScores.Values
            .Where(state => state.Score < 0
                            && Tools.Generic.LongerAgoThan(state.FailStamp, FailedScoreIncreaseEvery));

        protected virtual void Dispose(bool disposing) {
            if (disposing)
                _scoreMonitor.Dispose();
            GC.SuppressFinalize(this);
        }

        ~ScoreMirrorSelector() {
            Dispose(false);
        }

        class HostState
        {
            public DateTime FailStamp;
            public int Score;
        }
    }

    [DoNotObfuscate]
    public class NoHostsAvailableException : Exception
    {
        public NoHostsAvailableException(string message, Exception inner) : base(message, inner) {}
    }
}
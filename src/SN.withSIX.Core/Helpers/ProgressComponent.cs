// <copyright company="SIX Networks GmbH" file="ProgressComponent.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SN.withSIX.Core.Extensions;

namespace SN.withSIX.Core.Helpers
{
    public interface IProgressLeaf : IProgressComponent, ITProgress
    {
        void Finish();
    }

    public class ProgressLeaf : IProgressLeaf
    {
        readonly object _dataLock = new object();
        private volatile bool _done;
        private double _progress;
        private long? _speed;
        private volatile bool _started;

        public ProgressLeaf(string title, int weight = 1) {
            Title = title;
            Weight = weight;
        }

        public bool Success { get; private set; }
        public bool Started => Done || _started;
        public string Title { get; }

        public void Update(long? speed, double progress) {
            lock (_dataLock) {
                _progress = progress;
                _speed = speed;
                _started = true;
            }
        }

        public long? Speed
        {
            get
            {
                lock (_dataLock)
                    return _speed;
            }
        }
        public double Progress
        {
            get
            {
                lock (_dataLock)
                    return _progress;
            }
        }
        public string StatusText => GetStatusText();
        public int Weight { get; }
        public bool Done
        {
            get { return _done; }
            private set { _done = value; }
        }

        public void Finish() {
            Succeed();
            End();
        }

        public Disposable Start() {
            lock (_dataLock)
                _started = true;
            return new Disposable(End);
        }

        public void Do(Action act) {
            using (Start()) {
                try {
                    act();
                } catch {
                    Fail();
                    throw;
                }
                Succeed();
            }
        }

        public async Task Do(Func<Task> act) {
            using (Start()) {
                try {
                    await act().ConfigureAwait(false);
                } catch {
                    Fail();
                    throw;
                }
                Succeed();
            }
        }

        void Fail() => End();

        void Succeed() {
            lock (_dataLock) {
                _progress = 100;
                Success = true;
            }
        }

        void End() {
            lock (_dataLock) {
                _speed = null;
                Done = true;
            }
        }

        string GetStatusText() => $"{Title} {this.GetProgressText()}%{this.GetSpeedText()}";
    }

    public class ProgressContainer : IProgressComponent
    {
        List<IProgressComponent> _components = new List<IProgressComponent>();
        private string _last;

        public ProgressContainer(string title, int weight = 1) {
            Title = title;
            Weight = weight;
        }

        int WeightTotal { get; set; }

        public long? Speed => GetSpeed();
        public string Title { get; }
        public double Progress => GetProgress();
        public string StatusText => GetStatusText();
        public int Weight { get; }
        public bool Done => GetDone();

        public bool Started => _components.Any(x => x.Started);

        string GetInterestingInfo() {
            try {
                var progressComponents = _components.OfType<ProgressComponent>().ToArray();
                var doneCount = progressComponents.Count(x => x.Done);
                var activeComponents =
                    progressComponents.Select(x => x.GetFirstActive()).Where(x => x != null).ToArray();
                var interesting =
                    activeComponents
                        .GroupBy(x => x.Title)
                        .OrderByDescending(x => x.Count())
                        .ThenBy(x => x.Key)
                        .ToArray();
                var activeText = string.Join(", ", interesting.Select(x => $"{x.Count()} {x.Key}"));
                if (!string.IsNullOrWhiteSpace(activeText))
                    activeText = $" ({activeText})";
                return
                    _last =
                        $"{doneCount}/{progressComponents.Length} done. {activeComponents.Length} active{activeText}.";
            } catch (InvalidOperationException) {
                return _last;
            }
        }

        public void AddComponents(IReadOnlyCollection<IProgressComponent> components) {
            lock (_components) {
                var newComponents = _components.Concat(components).ToList();
                foreach (var c in components)
                    WeightTotal += c.Weight;
                _components = newComponents;
            }
        }

        public void RemoveComponents(IReadOnlyCollection<IProgressComponent> components) {
            lock (_components) {
                var newComponents = _components.Except(components).ToList();
                foreach (var c in components)
                    WeightTotal -= c.Weight;
                _components = newComponents;
            }
        }

        double GetProgress()
            => Done ? 100 : GetProgressInternal();

        private double GetProgressInternal()
            => _components.Aggregate(0.0, (cur, x) => cur + x.Progress/(WeightTotal/(double) x.Weight));

        long? GetSpeed() => Done ? null : GetSpeedInternal();

        long? GetSpeedInternal() => _components.Any(x => x.Speed.HasValue)
            ? _components.Select(x => x.Speed)
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .Aggregate<long, long?>(0, (cur, x) => cur + x)
            : null;

        int GetDoneCount() => _components.Count(x => x.Done);
        bool GetDone() => _components.Any() && _components.All(x => x.Done);

        string GetStatusText() {
            var interestingInfo = GetInterestingInfo();
            var iInfo = !string.IsNullOrWhiteSpace(interestingInfo) ? $"\n{interestingInfo}" : "";
            return $"{Title} {this.GetProgressText()}%{this.GetSpeedText()}{iInfo}";
        }
    }

    public interface IProgressComponent : IProgressRead, ISpeedRead
    {
        int Weight { get; }
        string StatusText { get; }
        string Title { get; }
        bool Done { get; }
        bool Started { get; }
    }

    public class ProgressComponent : IProgressComponent
    {
        List<IProgressComponent> _components = new List<IProgressComponent>();
        private string _last = "";

        public ProgressComponent(string title, int weight = 1) {
            Title = title;
            Weight = weight;
        }

        int WeightTotal { get; set; }

        int DoneCount => GetDoneCount();

        public int CurrentStage => GetCurrentStage();

        public long? Speed => GetSpeed();

        public string Title { get; }
        public int Weight { get; }
        public double Progress => GetProgress();
        public string StatusText => GetStatusText();
        public bool Done => GetDone();
        public bool Started => _components.Any(x => x.Started);

        public IEnumerable<IProgressComponent> GetComponents() => _components;

        long? GetSpeed() => Done ? null : GetSpeedInternal();

        long? GetSpeedInternal() => _components.Any(x => x.Speed.HasValue)
            ? _components.Select(x => x.Speed)
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .Aggregate<long, long?>(0, (cur, x) => cur + x)
            : null;

        bool GetDone() => _components.Any() && _components.All(x => x.Done);

        private string GetStatusText() {
            try {
                var activeTitle = GetStatusTitle();
                var iInfo = !string.IsNullOrWhiteSpace(activeTitle) ? $"\n{activeTitle}" : "";
                return _last = $"{Title} {GetCurrentStage()}/{_components.Count} {this.GetProgressText()}%{iInfo}";
            } catch (InvalidOperationException) {
                return _last;
            }
        }

        private string GetStatusTitle() {
            if (!_components.Any())
                return "Initializing";
            var active = GetFirstActive();
            var activeTitle = active == null ? "Done" : active.StatusText;
            return activeTitle;
        }

        private int GetCurrentStage() => Math.Min(DoneCount + 1, _components.Count);

        public IProgressComponent GetFirstActive() => _components.FirstOrDefault(x => !x.Done && x.Started);

        private IProgressComponent GetDeepestActive() {
            var first = GetFirstActive();
            var firstComponent = first as ProgressComponent;
            return firstComponent != null ? firstComponent.GetDeepestActive() : first;
        }

        double GetProgress() => Done ? 100 : GetProgressInternal();

        private double GetProgressInternal()
            => _components.Aggregate(0.0, (cur, x) => cur + x.Progress/(WeightTotal/(double) x.Weight));

        int GetDoneCount() => _components.Count(x => x.Done);

        public void AddComponents(IReadOnlyCollection<IProgressComponent> components) {
            lock (_components) {
                var newComponents = _components.Concat(components).ToList();
                foreach (var c in components)
                    WeightTotal += c.Weight;
                _components = newComponents;
            }
        }

        public void RemoveComponents(IReadOnlyCollection<IProgressComponent> components) {
            lock (_components) {
                var newComponents = _components.Except(components).ToList();
                foreach (var c in components)
                    WeightTotal -= c.Weight;
                _components = newComponents;
            }
        }
    }

    public static class ProgressComponentExtensions
    {
        public static void AddComponents(this ProgressComponent This, params IProgressComponent[] components)
            => This.AddComponents(components);

        public static void AddComponents(this ProgressContainer This, params IProgressComponent[] components)
            => This.AddComponents(components);

        public static void RemoveComponents(this ProgressComponent This, params IProgressComponent[] components)
            => This.RemoveComponents(components);

        public static void RemoveComponents(this ProgressContainer This, params IProgressComponent[] components)
            => This.RemoveComponents(components);

        public static string GetProgressText(this IProgressComponent This) => This.Progress.ToString("0.##");

        public static string GetSpeedText(this IProgressComponent This)
            => This.Speed == null ? "" : $" @ {This.Speed.Value.FormatSize()}/s";

        public static IProgressLeaf Finished(this IProgressLeaf This) {
            This.Finish();
            return This;
        }

        public static double ToProgress(this int count, int total) => count/(double) total*100;
        public static double ToProgress(this long count, long total) => count/(double) total*100;
        public static double ToProgress(this uint count, uint total) => count/(double) total*100;
        public static double ToProgress(this ulong count, ulong total) => count/(double) total*100;
        public static double ToProgress(this double count, int total) => count/total*100;
    }
}
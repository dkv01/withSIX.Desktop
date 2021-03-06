// <copyright company="SIX Networks GmbH" file="ProcessManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Management;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using withSIX.Api.Models.Extensions;
using withSIX.Core.Extensions;
using withSIX.Core.Helpers;
using withSIX.Core.Infra.Services;
using withSIX.Core.Logging;
using withSIX.Core.Services.Infrastructure;
using Timer = withSIX.Core.Helpers.Timer;

namespace withSIX.Core.Presentation.Bridge.Services
{
    public static class ProcessExtensions
    {
        public static ProcessStartInfo EnableRunAsAdministrator(this ProcessStartInfo arg) {
            if (Environment.OSVersion.Version.Major >= 6)
                arg.Verb = "runas";

            return arg;
        }

        public static ProcessManager.ProcessState MonitorProcessOutput(this Process process) {
            if (process == null) throw new ArgumentNullException(nameof(process));

            var state = new ProcessManager.ProcessState();

            if (!process.StartInfo.RedirectStandardOutput)
                throw new InvalidOperationException("Not redirected output");
            if (!process.StartInfo.RedirectStandardError)
                throw new InvalidOperationException("Not redirected error");

            process.OutputDataReceived += (sender, args) => state.UpdateStamp();
            process.ErrorDataReceived += (sender, args) => state.UpdateStamp();

            return state;
        }
    }

    public static class ReactiveProcessExtensions
    {
        public static ProcessManager.ProcessState MonitorProcessOutput(this ReactiveProcess process) {
            if (process == null) throw new ArgumentNullException(nameof(process));

            var state = new ProcessManager.ProcessState();

            if (!process.StartInfo.RedirectStandardOutput)
                throw new InvalidOperationException("Not redirected output");
            if (!process.StartInfo.RedirectStandardError)
                throw new InvalidOperationException("Not redirected error");

            // we terminate the observables so we dont have to dispose these subscriptions
            process.StandardOutputObservable.Subscribe(_ => state.UpdateStamp());
            process.StandardErrorObservable.Subscribe(_ => state.UpdateStamp());
            return state;
        }
    }

    public class ProcessManager : IProcessManager, IDisposable, IPresentationService // Platform Service ?
    {
        static readonly TimeSpan monitorInterval = TimeSpan.FromSeconds(1);
        static readonly TimeSpan defaultMonitorOutputTimeout = TimeSpan.FromHours(1);
        static readonly TimeSpan defaultMonitorRespondingTimeout = TimeSpan.FromMinutes(2);
        readonly ISubject<Tuple<ProcessStartInfo, int>, Tuple<ProcessStartInfo, int>> _launched;
        readonly
            ISubject<Tuple<ProcessStartInfo, int, TimeSpan, string>, Tuple<ProcessStartInfo, int, TimeSpan, string>>
            _monitorKilled;
        readonly ISubject<Tuple<ProcessStartInfo, int, string>, Tuple<ProcessStartInfo, int, string>> _monitorStarted;
        readonly ISubject<Tuple<ProcessStartInfo, int, string>, Tuple<ProcessStartInfo, int, string>> _monitorStopped;
        readonly CompositeDisposable _subjects = new CompositeDisposable();
        readonly ISubject<Tuple<ProcessStartInfo, int, int>, Tuple<ProcessStartInfo, int, int>> _terminated;

        public ProcessManager() {
            var launched = new Subject<Tuple<ProcessStartInfo, int>>();
            _launched = Subject.Synchronize(launched);
            Launched = _launched.AsObservable();
            _subjects.Add(launched);

            var killed = new Subject<Tuple<ProcessStartInfo, int, TimeSpan, string>>();
            _monitorKilled = Subject.Synchronize(killed);
            MonitorKilled = _monitorKilled.AsObservable();
            _subjects.Add(killed);

            var started = new Subject<Tuple<ProcessStartInfo, int, string>>();
            _monitorStarted = Subject.Synchronize(started);
            MonitorStarted = _monitorStarted.AsObservable();
            _subjects.Add(started);

            var stopped = new Subject<Tuple<ProcessStartInfo, int, string>>();
            _monitorStopped = Subject.Synchronize(stopped);
            MonitorStopped = _monitorStopped.AsObservable();
            _subjects.Add(stopped);

            var terminated = new Subject<Tuple<ProcessStartInfo, int, int>>();
            _terminated = Subject.Synchronize(terminated);
            Terminated = _terminated.AsObservable();
            _subjects.Add(terminated);

            Management = new ManagementInternal();
        }

        public void Dispose() => _subjects.Dispose();

        public IManagement Management { get; }

        public IObservable<Tuple<ProcessStartInfo, int>> Launched { get; }
        public IObservable<Tuple<ProcessStartInfo, int, TimeSpan, string>> MonitorKilled { get; }
        public IObservable<Tuple<ProcessStartInfo, int, string>> MonitorStarted { get; }
        public IObservable<Tuple<ProcessStartInfo, int, string>> MonitorStopped { get; }
        public IObservable<Tuple<ProcessStartInfo, int, int>> Terminated { get; }
        public TimeSpan DefaultMonitorOutputTimeOut => defaultMonitorOutputTimeout;
        public TimeSpan DefaultMonitorRespondingTimeOut => defaultMonitorRespondingTimeout;

        CompositeDisposable StartProcess(Process process, TimeSpan? monitorOutput = null,
            TimeSpan? monitorResponding = null) {
            process.Validate();
            process.Start();

            _launched.OnNext(Tuple.Create(process.StartInfo, process.Id));

            return SetupMonitoringDisposable(process, monitorOutput, monitorResponding);
        }

        CompositeDisposable SetupMonitoringDisposable(ReactiveProcess process, TimeSpan? monitorOutput,
            TimeSpan? monitorResponding) {
            var disposable = new CompositeDisposable();
            if (monitorOutput.HasValue)
                disposable.Add(MonitorProcessOutput(process, monitorOutput.Value));
            if (monitorResponding.HasValue)
                disposable.Add(MonitorProcessResponding(process, monitorResponding.Value));

            return disposable;
        }

        CompositeDisposable SetupMonitoringDisposable(Process process, TimeSpan? monitorOutput,
            TimeSpan? monitorResponding) {
            var disposable = new CompositeDisposable();
            if (monitorOutput.HasValue)
                disposable.Add(MonitorProcessOutput(process, monitorOutput.Value));
            if (monitorResponding.HasValue)
                disposable.Add(MonitorProcessResponding(process, monitorResponding.Value));

            return disposable;
        }

        public class ManagementInternal : IManagement, IEnableLogging
        {
            public virtual Process[] FindProcess(string name, string path = null) {
                if (name == null) throw new ArgumentNullException(nameof(name));
                return Process.GetProcessesByName(name.Replace(".exe", string.Empty));
            }

            public async Task WaitForExitALittleMore(string procName, int timeoutSeconds = int.MaxValue) {
                await Task.Run(() => WaitForExit(procName, timeoutSeconds)).ConfigureAwait(false);
                await Task.Delay(2*1000).ConfigureAwait(false);
                // to fix file in use.... grr, can we monitor locks of the file instead??
            }

            public void WaitForExit(string procName, int timeoutSeconds = int.MaxValue) {
                if (timeoutSeconds != int.MaxValue)
                    timeoutSeconds *= 1000;
                var cProcId = Process.GetCurrentProcess().Id;
                var proc = FindProcess(procName).FirstOrDefault(iproc => iproc.Id != cProcId);
                if (proc == null)
                    return;
                if (!proc.WaitForExit(timeoutSeconds)) {
                    throw new TimeoutException(
                        $"Process '{procName}' did not exit within the specified timeout of {timeoutSeconds} seconds.");
                }
            }

            public virtual void Kill(int pid, bool gracefully = false) {
                using (var proc = Process.GetProcessById(pid))
                    KillProcess(proc, gracefully);
            }

            public void SetAffinity(Process process, IEnumerable<int> usedProcessors) {
                var newAffinity = usedProcessors.Aggregate(0, (current, item) => current | (1 << (item - 1)));
                process.ProcessorAffinity = (IntPtr) newAffinity;
            }

            public virtual void KillProcess(Process p, bool gracefully = false) {
                if (p == null) throw new ArgumentNullException(nameof(p));
                if (gracefully) {
                    p.CloseMainWindow();
                    var i = 0;
                    while (!p.SafeHasExited()) {
                        i++;
                        if (i > 4)
                            break;
                        Thread.Sleep(1000);
                    }
                }

                if (p.SafeHasExited())
                    return;
                p.Kill();
            }

            public virtual bool KillByName(string name, string path = null, bool gracefully = false) {
                if (name == null) throw new ArgumentNullException(nameof(name));
                if (!(!string.IsNullOrWhiteSpace(name))) throw new ArgumentException("!string.IsNullOrWhiteSpace(name)");
                var processes = FindProcess(name, path);
                foreach (var p in processes) {
                    using (p) {
                        try {
                            KillProcess(p, gracefully);
                        } catch (Exception e) {
                            this.Logger().FormattedErrorException(e);
                        }
                    }
                }

                return processes.Any();
            }

            public virtual void KillProcessInclChildren(int pid, bool gracefully = false) {
                // http://msdn.microsoft.com/en-us/library/aa394372(v=vs.85).aspx
                try {
                    Kill(pid, gracefully);
                } finally {
                    KillProcessChildren(pid, gracefully);
                }
            }


            public virtual bool Running(string exe) {
                if (exe == null) throw new ArgumentNullException(nameof(exe));
                if (!(!string.IsNullOrWhiteSpace(exe))) throw new ArgumentException("!string.IsNullOrWhiteSpace(exe)");

                return GetRunningProcesses(exe).Any();
            }

            public Process[] GetRunningProcesses(string exe)
                => Process.GetProcessesByName(exe.Replace(".exe", string.Empty));

            public virtual void KillProcessChildren(int pid, bool gracefully = false) {
                var processes = GetWmiProcessesByParentId(pid);
                using (processes) {
                    foreach (var p in processes) {
                        using (p) {
                            try {
                                KillProcessInclChildren((int) (uint) p["ProcessId"]);
                            } catch (Exception e) {
                                this.Logger().FormattedErrorException(e);
                            }
                        }
                    }
                }
            }

            public virtual void KillNamedProcessChildren(string name, int pid, bool gracefully = false) {
                var processes = GetNamedWmiProcessesByParentId(name, pid);

                using (processes) {
                    foreach (var p in processes) {
                        using (p) {
                            try {
                                KillProcessInclChildren((int) (uint) p["ProcessId"]);
                            } catch (Exception e) {
                                this.Logger().FormattedErrorException(e);
                            }
                        }
                    }
                }
            }

            public virtual string GetCommandlineArgs(Process process) {
                if (process == null) throw new ArgumentNullException(nameof(process));
                return GetCommandlineArgs(process.Id);
            }

            public virtual string GetCommandlineArgs(int id) {
                var wmiQuery = $"select CommandLine from Win32_Process where ProcessId='{id}'";
                using (var searcher = new ManagementObjectSearcher(wmiQuery))
                using (var retObjectCollection = searcher.Get()) {
                    if (retObjectCollection.Count == 0)
                        return null;

                    foreach (ManagementObject retObject in retObjectCollection) {
                        using (retObject)
                            return (string) retObject["CommandLine"];
                    }
                }
                return null;
            }

            public virtual Dictionary<Process, string> GetCommandlineArgs(string name) {
                if (name == null) throw new ArgumentNullException(nameof(name));
                if (!(!string.IsNullOrWhiteSpace(name))) throw new ArgumentException("!string.IsNullOrWhiteSpace(name)");

                var parsedName = name.EndsWith(".exe") ? Path.GetFileNameWithoutExtension(name) : name;
                var procs = FindProcess(parsedName);
                if ((procs == null) || !procs.Any())
                    return new Dictionary<Process, string>();

                return procs
                    .ToDictionary(proc => proc, GetCommandlineArgs);
            }


            public IAbsoluteFilePath GetProcessPath(int processId) {
                var query = "SELECT ExecutablePath FROM Win32_Process WHERE ProcessId = " + processId;

                using (var mos = new ManagementObjectSearcher(query)) {
                    using (var moc = mos.Get()) {
                        var executablePath =
                            (from mo in moc.Cast<ManagementObject>() select mo["ExecutablePath"]).FirstOrDefault(
                                x => x != null);
                        var s = executablePath as string;
                        return s?.ToAbsoluteFilePath();
                    }
                }
            }

            public IEnumerable<Tuple<Process, IAbsoluteFilePath>> GetExecuteablePaths(string exe)
                => GetRunningProcesses(exe.Replace(".exe", string.Empty))
                    .Select(x => Tuple.Create(x, GetProcessPath(x.Id)));

            public void AddEnvironmentVariables(ProcessStartInfo info, IDictionary<string, string> vars) {
                foreach (var kvp in vars)
                    info.EnvironmentVariables[kvp.Key] = kvp.Value;
            }

            public virtual ManagementObjectCollection GetWmiProcessesByParentId(int pid)
                => new ManagementObjectSearcher(
                        $"Select * From Win32_Process Where ParentProcessID={pid}")
                    .Get();

            public virtual ManagementObjectCollection GetNamedWmiProcessesByParentId(string name, int pid)
                => new ManagementObjectSearcher(
                        $"Select * From Win32_Process Where ParentProcessID={pid} And Name=\"{name}\"")
                    .Get();

            public virtual ManagementObjectCollection GetWmiProcessesById(int pid)
                => new ManagementObjectSearcher("Select * From Win32_Process Where ProcessID=#{id}").Get();
        }

        #region Monitor

        Timer MonitorProcessOutput(ReactiveProcess process, TimeSpan timeout) {
            if (process == null) throw new ArgumentNullException(nameof(process));
            if (timeout == null) throw new ArgumentNullException(nameof(timeout));

            var state = process.MonitorProcessOutput();
            _monitorStarted.OnNext(Tuple.Create(process.StartInfo, process.Id, "Output"));
            return new TimerWithElapsedCancellation(monitorInterval,
                () => OnOutputMonitorElapsed(process, state, timeout),
                () => _monitorStopped.OnNext(Tuple.Create(process.StartInfo, process.Id, "Output")));
        }

        Timer MonitorProcessOutput(Process process, TimeSpan timeout) {
            if (process == null) throw new ArgumentNullException(nameof(process));
            if (timeout == null) throw new ArgumentNullException(nameof(timeout));

            var state = process.MonitorProcessOutput();
            _monitorStarted.OnNext(Tuple.Create(process.StartInfo, process.Id, "Output"));
            return new TimerWithElapsedCancellation(monitorInterval,
                () => OnOutputMonitorElapsed(process, state, timeout),
                () => _monitorStopped.OnNext(Tuple.Create(process.StartInfo, process.Id, "Output")));
        }

        bool OnOutputMonitorElapsed(Process process, ProcessState state, TimeSpan timeout) {
            if (TryCheckHasExited(process))
                return false;

            var lastResponse = state.Stamp;
            if (!Tools.Generic.LongerAgoThan(lastResponse, timeout))
                return true;

            var lastStreamLengths = state.StreamLengths;
            state.StreamLengths = Tuple.Create(TryGetLength(process.StandardOutput.BaseStream),
                TryGetLength(process.StandardError.BaseStream));
            if ((state.StreamLengths.Item1 != lastStreamLengths.Item1)
                || (state.StreamLengths.Item2 != lastStreamLengths.Item2)) {
                state.UpdateStamp();
                return true;
            }

            TryKillDueToNoOutputReceivedTimeout(process, timeout);
            return false;
        }

        private static long TryGetLength(Stream stdOut) {
            try {
                return stdOut.Length;
            } catch (NotSupportedException) {
                return 0;
            }
        }

        void TryKillDueToNoOutputReceivedTimeout(Process process, TimeSpan timeout) {
            try {
                process.Kill();
            } finally {
                _monitorKilled.OnNext(Tuple.Create(process.StartInfo, process.Id, timeout, "Output"));
            }
        }

        static bool TryCheckHasExited(Process process) {
            try {
                return process.SafeHasExited();
            } catch {
                return true;
            }
        }

        Timer MonitorProcessResponding(Process process, TimeSpan timeout) {
            if (process == null) throw new ArgumentNullException(nameof(process));
            if (timeout == null) throw new ArgumentNullException(nameof(timeout));

            var state = new ProcessState();

            _monitorStarted.OnNext(Tuple.Create(process.StartInfo, process.Id, "Responding"));

            return new TimerWithElapsedCancellation(monitorInterval, () => OnMonitorElapsed(process, state, timeout),
                () => _monitorStopped.OnNext(Tuple.Create(process.StartInfo, process.Id, "Responding")));
        }

        bool OnMonitorElapsed(Process process, ProcessState state, TimeSpan timeout) {
            var hasExited = TryCheckHasExited(process);

            if (hasExited)
                return false;

            if (TryCheckResponding(process)) {
                state.UpdateStamp();
                return true;
            }

            if (Tools.Generic.LongerAgoThan(state.Stamp, timeout))
                TryKillDueNotRespondingTimeout(process, timeout);
            return false;
        }

        void TryKillDueNotRespondingTimeout(Process process, TimeSpan timeout) {
            try {
                process.Kill();
            } finally {
                _monitorKilled.OnNext(Tuple.Create(process.StartInfo, process.Id, timeout, "Responding"));
            }
        }

        static bool TryCheckResponding(Process process) {
            var responding = false;
            try {
                responding = process.Responding;
            } catch {}
            return responding;
        }

        public class ProcessState
        {
            public ProcessState() {
                Stamp = Tools.Generic.GetCurrentUtcDateTime;
                StreamLengths = new Tuple<long, long>(0, 0);
            }

            public Tuple<long, long> StreamLengths { get; set; }
            public DateTime Stamp { get; private set; }

            public void UpdateStamp() {
                Stamp = Tools.Generic.GetCurrentUtcDateTime;
            }
        }

        #endregion

        #region Async variants

        public async Task<ProcessExitResult> LaunchAsync(BasicLaunchInfo info) {
            ProcessBLI(info);
            using (var process = new ReactiveProcess {StartInfo = info.StartInfo}) {
                return
                    await
                        LaunchAndWaitForExitAsync(process, info.MonitorOutput, info.MonitorResponding,
                                info.CancellationToken)
                            .ConfigureAwait(false);
            }
        }

        public async Task<ProcessExitResult> LaunchElevatedAsync(BasicLaunchInfo info) {
            info.StartInfo.EnableRunAsAdministrator();
            ProcessBLI(info);
            using (var process = new ReactiveProcess {StartInfo = info.StartInfo}) {
                return
                    await
                        LaunchAndWaitForExitAsync(process, info.MonitorOutput, info.MonitorResponding,
                                info.CancellationToken)
                            .ConfigureAwait(false);
            }
        }

        public async Task<ProcessExitResult> LaunchAndProcessAsync(LaunchAndProcessInfo info) {
            using (var process = new ReactiveProcess {StartInfo = info.StartInfo.EnableRedirect()}) {
                using (SetupStandardOutput(info, process))
                using (SetupStandardError(info, process))
                    return
                        await
                            LaunchAndWaitForExitAsync(process, info.MonitorOutput, info.MonitorResponding,
                                    info.CancellationToken)
                                .ConfigureAwait(false);
            }
        }

        static IDisposable SetupStandardError(LaunchAndProcessInfo info, ReactiveProcess process) {
            if (!info.StartInfo.RedirectStandardError)
                throw new InvalidOperationException("Not redirected error");

            var dsp = new CompositeDisposable();
            if (info.StandardErrorObs != null)
                dsp.Add(info.StandardErrorObs(process.StandardErrorObservable));
            if (info.StandardErrorAction != null)
                dsp.Add(process.StandardErrorObservable.Subscribe(data => info.StandardErrorAction(process, data)));
            return dsp;
        }

        static IDisposable SetupStandardOutput(LaunchAndProcessInfo info, ReactiveProcess process) {
            if (!info.StartInfo.RedirectStandardOutput)
                throw new InvalidOperationException("Not redirected output");
            var dsp = new CompositeDisposable();
            if (info.StandardOutputObs != null)
                dsp.Add(info.StandardOutputObs(process.StandardOutputObservable));
            if (info.StandardOutputAction != null)
                dsp.Add(process.StandardOutputObservable.Subscribe(data => info.StandardOutputAction(process, data)));
            return dsp;
        }

        public async Task<ProcessExitResultWithOutput> LaunchAndGrabAsync(BasicLaunchInfo info) {
            ProcessBLI(info);
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            var outInfo =
                await
                    LaunchAndProcessAsync(new LaunchAndProcessInfo(info.StartInfo.EnableRedirect()) {
                        StandardOutputAction = (process, data) => outputBuilder.AppendLine(data),
                        StandardErrorAction = (process, data) => errorBuilder.AppendLine(data),
                        MonitorOutput = info.MonitorOutput,
                        MonitorResponding = info.MonitorResponding
                    }).ConfigureAwait(false);
            return new ProcessExitResultWithOutput(outInfo.ExitCode, outInfo.Id, info.StartInfo,
                outputBuilder.ToString(),
                errorBuilder.ToString());
        }

        async Task<ProcessExitResult> LaunchAndWaitForExitAsync(ReactiveProcess process, TimeSpan? monitorOutput,
            TimeSpan? monitorResponding) {
            var task = process.StartAsync();
            _launched.OnNext(Tuple.Create(process.StartInfo, process.Id));

            using (SetupMonitoringDisposable(process, monitorOutput, monitorResponding))
                await task.ConfigureAwait(false);
            _terminated.OnNext(Tuple.Create(process.StartInfo, process.ExitCode, process.Id));
            return new ProcessExitResult(process.ExitCode, process.Id, process.StartInfo);
        }

        async Task<ProcessExitResult> LaunchAndWaitForExitAsync(ReactiveProcess process, TimeSpan? monitorOutput,
            TimeSpan? monitorResponding, CancellationToken token) {
            var task = process.StartAsync();
            _launched.OnNext(Tuple.Create(process.StartInfo, process.Id));

            using (SetupMonitoringDisposable(process, monitorOutput, monitorResponding))
            using (token.Register(process.TryKill))
                await task.ConfigureAwait(false);
            _terminated.OnNext(Tuple.Create(process.StartInfo, process.ExitCode, process.Id));
            token.ThrowIfCancellationRequested();
            return new ProcessExitResult(process.ExitCode, process.Id, process.StartInfo);
        }

        #endregion

        #region Sync Variants

        public Process Start(ProcessStartInfo startInfo) {
            startInfo.Validate();
            var process = Process.Start(startInfo);
            _launched.OnNext(Tuple.Create(startInfo, process?.Id ?? -1));
            return process;
        }

        public void StartAndForget(ProcessStartInfo startInfo) {
            using (Start(startInfo)) {}
        }

        public Process StartElevated(ProcessStartInfo startInfo) {
            startInfo.Validate();
            startInfo.Verb = "runas";
            var process = Process.Start(startInfo);
            _launched.OnNext(Tuple.Create(startInfo, process?.Id ?? -1));
            return process;
        }

        public void StartAndForgetElevated(ProcessStartInfo startInfo) {
            startInfo.Verb = "runas";
            using (Start(startInfo)) {}
        }


        public ProcessExitResult Launch(BasicLaunchInfo info) {
            ProcessBLI(info);
            using (var process = new Process {StartInfo = info.StartInfo}) {
                LaunchAndWaitForExit(process, info.MonitorOutput, info.MonitorResponding);
                return new ProcessExitResult(process.ExitCode, process.Id, info.StartInfo);
            }
        }

        public ProcessExitResult LaunchElevated(BasicLaunchInfo info) {
            info.StartInfo.EnableRunAsAdministrator();
            ProcessBLI(info);
            using (var process = new Process {StartInfo = info.StartInfo}) {
                LaunchAndWaitForExit(process, info.MonitorOutput, info.MonitorResponding);
                return new ProcessExitResult(process.ExitCode, process.Id, info.StartInfo);
            }
        }

        private static void ProcessBLI(BasicLaunchInfo info) {
            if (info.StartMinimized)
                info.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
        }

        public ProcessExitResultWithOutput LaunchAndGrab(BasicLaunchInfo info) {
            ProcessBLI(info);
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            var outInfo = LaunchAndProcess(new LaunchAndProcessInfo(info.StartInfo.EnableRedirect()) {
                StandardOutputAction = (process, data) => outputBuilder.AppendLine(data),
                StandardErrorAction = (process, data) => errorBuilder.AppendLine(data),
                MonitorOutput = info.MonitorOutput,
                MonitorResponding = info.MonitorResponding
            });
            return new ProcessExitResultWithOutput(outInfo.ExitCode, outInfo.Id, info.StartInfo,
                outputBuilder.ToString(),
                errorBuilder.ToString());
        }

        public virtual ProcessExitResultWithOutput LaunchAndGrabTool(ProcessStartInfo startInfo, string tool = null) {
            var ret = LaunchAndGrab(new BasicLaunchInfo(startInfo));
            if (ret.ExitCode == 0)
                return ret;

            throw BuildProcessException(startInfo, tool, ret);
        }

        private static ProcessException BuildProcessException(ProcessStartInfo startInfo, string tool,
            ProcessExitResultWithOutput ret) => new ProcessException(
            string.Format("{6} [{7}] error: {0} while running: {1} {2} from {3}\nOutput: {4}\nError: {5}",
                ret.ExitCode,
                startInfo.FileName,
                startInfo.Arguments,
                startInfo.WorkingDirectory,
                ret.StandardOutput,
                ret.StandardError,
                tool ?? Path.GetFileNameWithoutExtension(startInfo.FileName),
                ret.Id));

        public virtual ProcessExitResultWithOutput LaunchAndGrabToolCmd(ProcessStartInfo info,
            string tool) {
            var startInfo =
                new ProcessStartInfoBuilder(Common.Paths.CmdExe,
                    $"/C \"\"{info.FileName}\" {info.Arguments}\"") {
                    WorkingDirectory = info.WorkingDirectory.ToAbsoluteDirectoryPathNullSafe()
                }.Build();
            return LaunchAndGrabTool(startInfo, tool);
        }

        public ProcessExitResult LaunchAndProcess(LaunchAndProcessInfo info)
            => LaunchAndProcessAsync(info).WaitAndUnwrapException();

        void LaunchAndWaitForExit(Process process, TimeSpan? monitorOutput = null, TimeSpan? monitorResponding = null) {
            var waitHandler = new WaitForExitHandler(process);
            using (StartProcess(process, monitorOutput, monitorResponding))
                waitHandler.WaitForExit();
            _terminated.OnNext(Tuple.Create(process.StartInfo, process.ExitCode, process.Id));
        }

        class AsyncWaitForExitHandler : IDisposable
        {
            Process _process;
            Task _task;
            TaskCompletionSource<object> _tcs;
            TaskCompletionSource<object> _tcsError;
            TaskCompletionSource<object> _tcsOutput;

            public AsyncWaitForExitHandler(Process process,
                CancellationToken cancellationToken = default(CancellationToken)) {
                _process = process;
                _tcs = new TaskCompletionSource<object>();

                // Best to attach these handlers last because that should mean we have processed all data
                // Unclear however what happens if there is an error in any of the earlier handlers...
                if (process.StartInfo.RedirectStandardOutput)
                    SetupOutput();
                if (process.StartInfo.RedirectStandardError)
                    SetupError();

                var tcs = new[] {_tcs, _tcsError, _tcsOutput}.Where(x => x != null).ToArray();
                if (cancellationToken != default(CancellationToken)) {
                    foreach (var t in tcs)
                        cancellationToken.Register(() => t.TrySetCanceled());
                }

                process.EnableRaisingEvents = true;
                process.Exited += Exited;
                _task = Task.WhenAll(tcs.Select(x => x.Task));
            }

            public void Dispose() {
                _process.OutputDataReceived -= OnProcessOnOutputDataReceived;
                _process.ErrorDataReceived -= OnProcessOnErrorDataReceived;
                _process.Exited -= Exited;
                _process.EnableRaisingEvents = false;
                _process = null;
                _tcsOutput = null;
                _tcsError = null;
                _tcs = null;
                _task = null;
            }

            public Task WaitForExit() {
                if (_process.StartInfo.RedirectStandardOutput)
                    _process.BeginOutputReadLine();
                if (_process.StartInfo.RedirectStandardError)
                    _process.BeginErrorReadLine();
                return _task;
            }

            void SetupOutput() {
                _tcsOutput = new TaskCompletionSource<object>();
                _process.OutputDataReceived += OnProcessOnOutputDataReceived;
            }

            void OnProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e) {
                if (e.Data != null)
                    return;
                _tcsOutput.TrySetResult(null);
            }

            void SetupError() {
                _tcsError = new TaskCompletionSource<object>();
                _process.ErrorDataReceived += OnProcessOnErrorDataReceived;
            }

            void OnProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e) {
                if (e.Data != null)
                    return;
                _tcsError.TrySetResult(null);
            }

            void Exited(object sender, EventArgs eventArgs) {
                // For some reason had to use TrySetResult instead of SetResult, because otherwise it might throw 'already ran to completion' error...
                _tcs.TrySetResult(null);
            }
        }

        class WaitForExitHandler
        {
            readonly Process _process;
            AutoResetEvent _errorWaitHandle;
            AutoResetEvent _outputWaitHandle;

            public WaitForExitHandler(Process process) {
                _process = process;
                // Best to attach these handlers last because that should mean we have processed all data
                // Unclear however what happens if there is an error in any of the earlier handlers...
                if (_process.StartInfo.RedirectStandardOutput)
                    process.OutputDataReceived += OnProcessOnOutputDataReceived;

                if (_process.StartInfo.RedirectStandardError)
                    process.ErrorDataReceived += OnProcessOnErrorDataReceived;
            }

            void OnProcessOnOutputDataReceived(object sender, DataReceivedEventArgs args) {
                if (args.Data != null)
                    return;
                _process.OutputDataReceived -= OnProcessOnOutputDataReceived;
                _outputWaitHandle.Set();
            }

            void OnProcessOnErrorDataReceived(object sender, DataReceivedEventArgs args) {
                if (args.Data != null)
                    return;
                _process.ErrorDataReceived -= OnProcessOnErrorDataReceived;
                _errorWaitHandle.Set();
            }

            public void WaitForExit() {
                using (_outputWaitHandle = _process.StartInfo.RedirectStandardOutput ? new AutoResetEvent(false) : null)
                using (
                    _errorWaitHandle = _process.StartInfo.RedirectStandardError ? new AutoResetEvent(false) : null) {
                    if (_process.StartInfo.RedirectStandardOutput)
                        _process.BeginOutputReadLine();
                    if (_process.StartInfo.RedirectStandardError)
                        _process.BeginErrorReadLine();
                    _process.WaitForExit();
                    if (_outputWaitHandle != null)
                        _outputWaitHandle.WaitOne();
                    if (_errorWaitHandle != null)
                        _errorWaitHandle.WaitOne();
                }
            }
        }

        #endregion
    }
}
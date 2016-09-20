// <copyright company="SIX Networks GmbH" file="IProcessManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace SN.withSIX.Core.Services.Infrastructure
{
    public interface IProcessManagerSync
    {
        Process Start(ProcessStartInfo startInfo);
        void StartAndForget(ProcessStartInfo startInfo);
        ProcessExitResult Launch(BasicLaunchInfo info);
        ProcessExitResult LaunchElevated(BasicLaunchInfo info);
        ProcessExitResultWithOutput LaunchAndGrab(BasicLaunchInfo info);
        ProcessExitResultWithOutput LaunchAndGrabTool(ProcessStartInfo info, string tool = null);
        ProcessExitResultWithOutput LaunchAndGrabToolCmd(ProcessStartInfo info, string tool);
        ProcessExitResult LaunchAndProcess(LaunchAndProcessInfo info);
    }

    [ContractClass(typeof (ContractClassForIProcessManager))]
    public interface IProcessManagerAsync
    {
        Task<ProcessExitResult> LaunchAsync(BasicLaunchInfo info);
        Task<ProcessExitResult> LaunchElevatedAsync(BasicLaunchInfo info);
        Task<ProcessExitResultWithOutput> LaunchAndGrabAsync(BasicLaunchInfo info);
        Task<ProcessExitResult> LaunchAndProcessAsync(LaunchAndProcessInfo info);
    }

    public interface IProcessManager : IProcessManagerSync, IProcessManagerAsync
    {
        IManagement Management { get; }
        TimeSpan DefaultMonitorOutputTimeOut { get; }
        TimeSpan DefaultMonitorRespondingTimeOut { get; }
        IObservable<Tuple<ProcessStartInfo, int>> Launched { get; }
        IObservable<Tuple<ProcessStartInfo, int, TimeSpan, string>> MonitorKilled { get; }
        IObservable<Tuple<ProcessStartInfo, int, string>> MonitorStarted { get; }
        IObservable<Tuple<ProcessStartInfo, int, string>> MonitorStopped { get; }
        IObservable<Tuple<ProcessStartInfo, int, int>> Terminated { get; }
    }

    [ContractClassFor(typeof (IProcessManagerAsync))]
    public abstract class ContractClassForIProcessManager : IProcessManagerAsync
    {
        public Task<ProcessExitResult> LaunchAsync(BasicLaunchInfo info) {
            Contract.Requires<ArgumentNullException>(info != null);
            return default(Task<ProcessExitResult>);
        }

        public abstract Task<ProcessExitResult> LaunchElevatedAsync(BasicLaunchInfo info);

        public Task<ProcessExitResultWithOutput> LaunchAndGrabAsync(BasicLaunchInfo info) {
            Contract.Requires<ArgumentNullException>(info != null);
            return default(Task<ProcessExitResultWithOutput>);
        }

        public Task<ProcessExitResult> LaunchAndProcessAsync(LaunchAndProcessInfo info) {
            Contract.Requires<ArgumentNullException>(info != null);
            return default(Task<ProcessExitResult>);
        }

        public abstract Process Launch(BasicLaunchInfo info);
    }

    public class BasicLaunchInfo
    {
        public BasicLaunchInfo(ProcessStartInfo startInfo) {
            StartInfo = startInfo;
        }

        public ProcessStartInfo StartInfo { get; set; }
        public TimeSpan? MonitorOutput { get; set; }
        public TimeSpan? MonitorResponding { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public bool StartMinimized { get; set; }
    }

    public class LaunchAndProcessInfo : BasicLaunchInfo
    {
        public LaunchAndProcessInfo(ProcessStartInfo startInfo) : base(startInfo) {}
        public Action<Process, string> StandardOutputAction { get; set; }
        public Action<Process, string> StandardErrorAction { get; set; }
        public Func<IObservable<string>, IDisposable> StandardOutputObs { get; set; }
        public Func<IObservable<string>, IDisposable> StandardErrorObs { get; set; }
    }
}
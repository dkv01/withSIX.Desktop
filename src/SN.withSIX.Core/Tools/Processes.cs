// <copyright company="SIX Networks GmbH" file="Processes.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NDepend.Path;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Core.Logging;

namespace SN.withSIX.Core
{
    public static partial class Tools
    {
        public static ProcessesTools Processes = new ProcessesTools();

        #region Nested type: Processes

        public class ProcessesTools : IEnableLogging
        {
            public virtual Process[] FindProcess(string name, string path = null) {
                Contract.Requires<ArgumentNullException>(name != null);
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
                Contract.Requires<ArgumentNullException>(p != null);
                if (gracefully) {
                    //p.CloseMainWindow();
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
                Contract.Requires<ArgumentNullException>(name != null);
                Contract.Requires<ArgumentException>(!string.IsNullOrWhiteSpace(name));
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
                Contract.Requires<ArgumentNullException>(exe != null);
                Contract.Requires<ArgumentException>(!string.IsNullOrWhiteSpace(exe));

                return GetRunningProcesses(exe).Any();
            }

            public Process[] GetRunningProcesses(string exe)
                => Process.GetProcessesByName(exe.Replace(".exe", string.Empty));

            public virtual void KillProcessChildren(int pid, bool gracefully = false) {
                ProcessManager.Management.KillProcessChildren(pid, gracefully);
            }
        }

        #endregion
    }

    public interface IManagement
    {
        void KillProcessChildren(int pid, bool gracefully = false);
        void KillNamedProcessChildren(string name, int pid, bool gracefully = false);
        string GetCommandlineArgs(Process process);
        string GetCommandlineArgs(int id);
        Dictionary<Process, string> GetCommandlineArgs(string name);
        IAbsoluteFilePath GetProcessPath(int processId);
        IEnumerable<Tuple<Process, IAbsoluteFilePath>> GetExecuteablePaths(string exe);
    }
}
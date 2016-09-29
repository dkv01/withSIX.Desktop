// <copyright company="SIX Networks GmbH" file="Processes.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NDepend.Path;

namespace withSIX.Core
{
    public interface IManagement
    {
        Process[] FindProcess(string name, string path = null);
        Task WaitForExitALittleMore(string procName, int timeoutSeconds = int.MaxValue);
        void WaitForExit(string procName, int timeoutSeconds = int.MaxValue);
        void Kill(int pid, bool gracefully = false);
        void SetAffinity(Process process, IEnumerable<int> usedProcessors);
        void KillProcess(Process p, bool gracefully = false);
        bool KillByName(string name, string path = null, bool gracefully = false);
        void KillProcessInclChildren(int pid, bool gracefully = false);
        bool Running(string exe);
        Process[] GetRunningProcesses(string exe);
        void KillProcessChildren(int pid, bool gracefully = false);
        void KillNamedProcessChildren(string name, int pid, bool gracefully = false);
        string GetCommandlineArgs(Process process);
        string GetCommandlineArgs(int id);
        Dictionary<Process, string> GetCommandlineArgs(string name);
        IAbsoluteFilePath GetProcessPath(int processId);
        IEnumerable<Tuple<Process, IAbsoluteFilePath>> GetExecuteablePaths(string exe);
        void AddEnvironmentVariables(ProcessStartInfo info, IDictionary<string, string> vars);
    }
}
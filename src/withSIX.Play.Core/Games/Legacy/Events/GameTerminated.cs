// <copyright company="SIX Networks GmbH" file="GameTerminated.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using withSIX.Play.Core.Connect.Events;

namespace withSIX.Play.Core.Games.Legacy.Events
{
    public class GameTerminated : TimeStampedEvent
    {
        public Process Process { get; }

        public GameTerminated(Process process) {
            if (process == null) throw new ArgumentNullException(nameof(process));

            Process = process;
        }
    }
}
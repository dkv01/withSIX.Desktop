// <copyright company="SIX Networks GmbH" file="ILogManager.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.CompilerServices;

namespace withSIX.Core.Logging
{
    public interface ILogManager
    {
        ILogger GetLogger(string type);
        ILogger GetCurrentClassLogger([CallerMemberName]string method = "");
        ILogger GetCurrentClassLogger(Type loggerType, [CallerMemberName]string method = "");
        ILogger GetCurrentClassLoggerOrMerged([CallerMemberName]string method = "");
        ILogger GetCurrentClassLoggerOrMerged(Type loggerType, [CallerMemberName]string method = "");
        ILogger GetLoggerOrMerged(Type type);
    }
}
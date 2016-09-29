// <copyright company="SIX Networks GmbH" file="LoggingSetup.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reflection;

namespace withSIX.Mini.Presentation.Electron
{
    public static class LoggingSetup
    {
        public static void Setup(string productTitle, string token = null) {
            var a = Assembly.LoadFrom("SN.withSIX.Core.Presentation.Bridge.dll");
            dynamic o = Activator.CreateInstance(a.GetType("SN.withSIX.Core.Presentation.Bridge.LowInitializer"));
            o.SetupLogging(productTitle, token);
        }
    }
}
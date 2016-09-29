// <copyright company="SIX Networks GmbH" file="LoggingSetup.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reflection;

namespace withSIX.Core.Presentation.Wpf
{
    public static class LoggingSetup
    {
        public static void Setup(string productTitle, string token = null) {
            var a = Assembly.LoadFrom("withSIX.Core.Presentation.Bridge.dll");
            dynamic o = Activator.CreateInstance(a.GetType("withSIX.Core.Presentation.Bridge.LowInitializer"));
            o.SetupLogging(productTitle, token);
        }
    }
}
﻿// <copyright company="SIX Networks GmbH" file="LoggingSetup.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Reflection;
using withSIX.Core.Presentation.Bridge;

namespace withSIX.Steam.Presentation
{
    public static class LoggingSetup
    {
        public static void Setup(string productTitle, string token = null) {
            //var a = Assembly.LoadFrom("withSIX.Core.Presentation.Bridge.dll");
            //dynamic o = Activator.CreateInstance(a.GetType("withSIX.Core.Presentation.Bridge.LowInitializer"));
            var o = new LowInitializer();
            o.SetupLogging(productTitle, token);
        }
    }
}
// <copyright company="SIX Networks GmbH" file="SixAwesomiumStart.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using CefSharp;
using withSIX.Core;
using withSIX.Play.Core.Options;
using withSIX.Api.Models.Extensions;

namespace withSIX.Play.Presentation.Wpf.Services
{
    public static class SixCefStart
    {
        static UserSettings _settings;
        static readonly string CustomCSS = @"
body {
  background-color: #f0f0f0;
}
::-webkit-scrollbar {
  width: 13px;
  height: 16px;
}

::-webkit-scrollbar-track-piece {
  background-color: #f0f0f0;
  background-clip: padding-box;
  border-color: transparant;
}

::-webkit-scrollbar-thumb:vertical {
  min-height: 16px;
  background-color: rgba(0, 0, 0, .2);
  background-clip: padding-box;
  border: 1px solid #eee;
}";

        public static void Initialize(UserSettings settings) {
            _settings = settings;
            Common.Paths.LogPath.MakeSurePathExists();
            var dataPath = Common.Paths.AwesomiumPath;
            dataPath.MakeSurePathExists();
            var cachePath = dataPath.GetChildDirectoryWithName("cache");
            cachePath.MakeSurePathExists();

            Cef.OnContextInitialized += WebCoreOnStarted;
            var cefSettings = new CefSettings {
                LogFile = Common.Paths.LogPath.GetChildFileWithName("cef.log").ToString(),
                UserDataPath = dataPath.ToString(),
                CachePath = cachePath.ToString() //,
                //PersistSessionCookies = true // flash but security hole..
#if DEBUG
                ,
                //RemoteDebuggingHost = "127.0.0.1",
                RemoteDebuggingPort = 6666
#endif
            };
            cefSettings.CefCommandLineArgs.Add("disable-gpu", "1");
            Cef.Initialize(cefSettings);
        }

        static void WebCoreOnStarted() {
            //WebCore.ResourceInterceptor = new SixResourceInterceptor(_settings);
        }

        [STAThread]
        public static void Exit() {
            if (Cef.IsInitialized)
                Cef.Shutdown();
        }
    }
}
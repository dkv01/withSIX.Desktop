// <copyright company="SIX Networks GmbH" file="BrowserInterop.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.InteropServices;
using Caliburn.Micro;
using SN.withSIX.Core.Services;

namespace SN.withSIX.Play.Core.Connect
{
    [ComVisible(true)]

    public class BrowserInterop : IDomainService
    {
        const int ApiVersion = 1;
        readonly IEventAggregator _eventBus;

        public BrowserInterop(IEventAggregator eventBus) {
            _eventBus = eventBus;
        }

        public int API_VERSION => ApiVersion;

        public void OpenPwsUri(string url) {
            _eventBus.PublishOnCurrentThread(new ProcessAppEvent(url));
        }

        public void RefreshLogin() {
            _eventBus.PublishOnCurrentThread(new RefreshLoginRequest());
        }
    }

    public class RefreshLoginRequest {}

    public class ProcessAppEvent
    {
        public ProcessAppEvent(string url) {
            URL = url;
        }

        public string URL { get; set; }
    }
}
// <copyright company="SIX Networks GmbH" file="CallContextService.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Remoting.Messaging;
using withSIX.Core.Applications.Services;

namespace withSIX.Core.Presentation.Bridge.Services
{
    public class CallContextService : ICallContextService, IPresentationService
    {
        public object GetIdentifier(string key) => GetIdentifierInternal(key);

        public void SetId(object id, string key) => SetIdInternal((InstanceIdentifier) id, key);

        public object Create() => CreateInternal();

        private static InstanceIdentifier GetIdentifierInternal(string key)
            => CallContext.LogicalGetData(key) as InstanceIdentifier;

        private static void SetIdInternal(InstanceIdentifier id, string key) => CallContext.LogicalSetData(key, id);

        private InstanceIdentifier CreateInternal() => new InstanceIdentifier();

        internal class InstanceIdentifier : MarshalByRefObject {}
    }
}
using System;
using System.Runtime.Remoting.Messaging;
using SN.withSIX.Core.Presentation;
using SN.withSIX.Mini.Infra.Data.Services;

namespace SN.withSIX.Mini.Presentation.Core.Services
{
    public class CallContextService : ICallContextService, IPresentationService
    {
        public object GetIdentifier(string key) => GetIdentifierInternal(key);

        private static InstanceIdentifier GetIdentifierInternal(string key)
            => CallContext.LogicalGetData(key) as InstanceIdentifier;

        internal class InstanceIdentifier : MarshalByRefObject { }

        public void SetId(object id, string key) => SetIdInternal((InstanceIdentifier) id, key);
        private static void SetIdInternal(InstanceIdentifier id, string key) => CallContext.LogicalSetData(key, id);

        public object Create() => CreateInternal();

        private InstanceIdentifier CreateInternal() => new InstanceIdentifier();
    }
}
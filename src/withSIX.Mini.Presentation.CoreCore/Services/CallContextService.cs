// <copyright company="SIX Networks GmbH" file="CallContextService.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading;
using withSIX.Core.Applications.Services;
using withSIX.Core.Presentation;

namespace withSIX.Mini.Presentation.CoreCore.Services
{
    public class CallContextService : ICallContextService, IPresentationService
    {
        private static AsyncLocal<InstanceIdentifier> Id = new AsyncLocal<InstanceIdentifier>();
        public object GetIdentifier(string key) => GetIdentifierInternal(key);

        public void SetId(object id, string key) => SetIdInternal((InstanceIdentifier) id, key);

        public object Create() => CreateInternal();

        private static InstanceIdentifier GetIdentifierInternal(string key)
            => Id.Value;

        private static void SetIdInternal(InstanceIdentifier id, string key) => Id.Value = id;

        private InstanceIdentifier CreateInternal() => new InstanceIdentifier();

        internal class InstanceIdentifier {}
    }
}
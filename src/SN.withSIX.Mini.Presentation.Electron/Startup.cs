// <copyright company="SIX Networks GmbH" file="Startup.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using SN.withSIX.Core.Extensions;
using SN.withSIX.Mini.Applications;

namespace SN.withSIX.Mini.Presentation.Electron
{
    public class StartupIsReady
    {
        public async Task<object> Invoke(dynamic input) => Cheat.Initialized;
    }

    public class Startup
    {
        public async Task<object> Invoke(dynamic input) {
            try {
                // doing this will stop the return of the promise...
                //await Task.Run(() => ByOther());
                var api = new NodeApi(input.api);

                ByOther(api);
                return true;
            } catch (Exception ex) {
                Console.WriteLine(ex.Format());
                throw;
            }
        }

        public void ByDomain(string path) {
            var dom = AppDomain.CreateDomain("MyDomain", null, path, string.Empty, false);
            dom.ExecuteAssemblyByName("Sync");
        }

        public void ByOther(NodeApi api) => Entrypoint.MainForNode(api);
    }
}
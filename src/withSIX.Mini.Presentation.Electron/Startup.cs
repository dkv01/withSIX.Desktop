// <copyright company="SIX Networks GmbH" file="Startup.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;
using withSIX.Core.Extensions;

namespace withSIX.Mini.Presentation.Electron
{
    public class Startup
    {
        public async Task<object> Invoke(dynamic input) {
            try {
                var api = new NodeApi(input.api);
                await Entrypoint.MainForNode(api).ConfigureAwait(false);
                return true;
            } catch (Exception ex) {
                Console.WriteLine(ex.Format());
                throw;
            }
        }

        /*
        public void ByDomain(string path) {
            var dom = AppDomain.CreateDomain("MyDomain", null, path, string.Empty, false);
            dom.ExecuteAssemblyByName("Sync");
        }
        */
    }
}
// <copyright company="SIX Networks GmbH" file="Teardown.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Threading.Tasks;

namespace withSIX.Mini.Presentation.Electron
{
    public class Teardown
    {
        public async Task<object> Invoke(object input) {
            Entrypoint.ExitForNode();
            return true;
        }
    }
}
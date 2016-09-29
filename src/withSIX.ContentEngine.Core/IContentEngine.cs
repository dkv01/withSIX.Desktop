// <copyright company="SIX Networks GmbH" file="IContentEngine.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Threading.Tasks;

namespace withSIX.ContentEngine.Core
{
    public interface IContentEngine
    {
        bool ModHasScript(Guid guid);
        Task LoadModS(IContentEngineContent mod, IContentEngineGame game, bool overrideMod = false);
        bool ModHasScript(IContentEngineContent mod);
    }
}
// <copyright company="SIX Networks GmbH" file="ContentsBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using SN.withSIX.Mini.Core.Games;

namespace SN.withSIX.Mini.Applications.Usecases
{
    public interface INeedContents {
        List<ContentGuidSpec> Contents { get; }
    }

    public abstract class ContentsBase : GameContentBaseWithInfo, INeedContents
    {
        protected ContentsBase(Guid gameId, List<ContentGuidSpec> contents) : base(gameId) {
            Contents = contents;
        }

        public List<ContentGuidSpec> Contents { get; }
    }

    public abstract class ContentsIntBase : GameContentBaseWithInfo
    {
        protected ContentsIntBase(Guid gameId, List<ContentIntSpec> contents) : base(gameId) {
            Contents = contents;
        }

        public List<ContentIntSpec> Contents { get; }
    }

    public interface IHaveRequestName
    {
        string Name { get; }
        Uri Href { get; }
    }
}
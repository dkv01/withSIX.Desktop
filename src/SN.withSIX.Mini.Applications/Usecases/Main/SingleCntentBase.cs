﻿// <copyright company="SIX Networks GmbH" file="SingleCntentBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using SN.withSIX.Core;
using withSIX.Api.Models.Content.v3;
using ContentGuidSpec = SN.withSIX.Mini.Core.Games.ContentGuidSpec;

namespace SN.withSIX.Mini.Applications.Usecases.Main
{
    public abstract class SingleCntentBase : GameContentBase, IHaveId<Guid>, IHaveContent
    {
        protected SingleCntentBase(Guid gameId, ContentGuidSpec content) : base(gameId) {
            Content = content;
        }

        public ContentGuidSpec Content { get; }
        public Guid Id => Content.Id;
    }

    public interface IHaveContent
    {
        ContentGuidSpec Content { get; }
    }
}
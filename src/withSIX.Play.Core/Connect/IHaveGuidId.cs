// <copyright company="SIX Networks GmbH" file="IHaveGuidId.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using withSIX.Api.Models;
using withSIX.Api.Models.Content.v3;

namespace withSIX.Play.Core.Connect
{
    public interface IHaveGuidId : IHaveId<Guid>, IComparePK<IHaveGuidId> {}
}
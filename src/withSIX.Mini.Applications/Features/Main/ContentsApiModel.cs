// <copyright company="SIX Networks GmbH" file="ContentsApiModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Collections.Generic;
using System.Runtime.Serialization;
using withSIX.Api.Models;
using withSIX.Api.Models.Extensions;

namespace withSIX.Mini.Applications.Usecases.Main
{
    [DataContract]
    public abstract class ContentsApiModel : PageModel<ContentApiModel>
    {
        protected ContentsApiModel(List<ContentApiModel> items, int total, int pageNumber, int pageSize) : base(items, new PagingInfo(pageNumber, total, pageSize)) {}
    }
}
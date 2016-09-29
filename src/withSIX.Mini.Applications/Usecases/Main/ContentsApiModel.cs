// <copyright company="SIX Networks GmbH" file="ContentsApiModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;
using withSIX.Api.Models;

namespace withSIX.Mini.Applications.Usecases.Main
{
    [DataContract]
    public abstract class ContentsApiModel : PageModel<ContentApiModel> {}
}
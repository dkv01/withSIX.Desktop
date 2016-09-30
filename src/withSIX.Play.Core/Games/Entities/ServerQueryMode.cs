// <copyright company="SIX Networks GmbH" file="ServerQueryMode.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;

namespace withSIX.Play.Core.Games.Entities
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/SN.withSIX.Play.Core")]

    public enum ServerQueryMode
    {
        [EnumMember] All = 0,
        [EnumMember] Gamespy = 1,
        [EnumMember] Steam = 2
    }
}
// <copyright company="SIX Networks GmbH" file="ServerQueryMode.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

//using System.Runtime.Serialization;

namespace GameServerQuery
{
    //[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Six.Core")]
    public enum ServerQueryMode
    {
        /* [EnumMember] */ All = 0,
        /* [EnumMember] */ Gamespy = 1,
        /* [EnumMember] */ Steam = 2
    }
}
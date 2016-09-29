// <copyright company="SIX Networks GmbH" file="CategoryDto.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Runtime.Serialization;


namespace SN.withSIX.Play.Infra.Api.ContentApi.Dto
{
    [DataContract]
    class CategoryDto : NewSyncBaseDto
    {
        [DataMember]
        public string Name { get; set; }
    }

    [DataContract]
    class CategoryRelationDto : NewRelationDtoBase {}
}
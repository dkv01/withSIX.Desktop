// <copyright company="SIX Networks GmbH" file="RelationDtoBase.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Runtime.Serialization;


namespace SN.withSIX.Play.Infra.Api.ContentApi.Dto
{
    [DataContract]
    public abstract class RelationDtoBase
    {
        [DataMember]
        public string Uuid { get; set; }
    }

    [DataContract]
    public abstract class NewRelationDtoBase
    {
        [DataMember]
        public Guid Id { get; set; }
        public string Uuid
        {
            get { return Id.ToString(); }
            set { Id = Guid.Parse(value); }
        }
    }
}
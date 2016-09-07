// <copyright company="SIX Networks GmbH" file="BaseEntity.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using SN.withSIX.Core;
using SN.withSIX.Core.Services.Infrastructure;
using withSIX.Api.Models.Content.v3;

namespace SN.withSIX.Mini.Core
{
    [DataContract]
    public abstract class BaseEntity
    {
        [IgnoreDataMember] readonly IDomainEventHandler _domainEventHandler = CoreCheat.EventGrabber.Get();

        protected void PrepareEvent(ISyncDomainEvent evt) => _domainEventHandler.PrepareEvent(this, evt);
    }

    [DataContract]
    public abstract class BaseEntity<TId> : BaseEntity, IHaveId<TId>, IEquatable<BaseEntity<TId>>
    {
        [DataMember]
        public TId Id { get; protected set; }

        public bool Equals(BaseEntity<TId> other) => other != null
                                                     &&
                                                     (ReferenceEquals(this, other) ||
                                                      (!EqualityComparer<TId>.Default.Equals(Id, default(TId)) &&
                                                       EqualityComparer<TId>.Default.Equals(Id, other.Id)));
        public override bool Equals(object other) => Equals(other as BaseEntity<TId>);

        public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);
    }

    [DataContract]
    public abstract class BaseEntityGuidId : BaseEntity<Guid>
    {
        protected BaseEntityGuidId() {
            Id = Guid.NewGuid();
        }
    }
}
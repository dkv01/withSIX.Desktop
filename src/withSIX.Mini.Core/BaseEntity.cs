﻿// <copyright company="SIX Networks GmbH" file="BaseEntity.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using withSIX.Api.Models.Content.v3;
using withSIX.Core;
using withSIX.Core.Services.Infrastructure;

namespace withSIX.Mini.Core
{
    [DataContract]
    public abstract class BaseEntity
    {
        [IgnoreDataMember] readonly IDomainEventHandler _domainEventHandler = CoreCheat.EventGrabber.Get();

        protected void PrepareEvent(ISyncDomainEvent evt) => _domainEventHandler.PrepareEvent(this, evt);

        protected Task RaiseRealtimeEvent(IDomainEvent evt) => _domainEventHandler.RaiseRealtimeEvent(evt);
    }

    [DataContract]
    public abstract class BaseEntity<TId> : BaseEntity, IHaveId<TId>, IEquatable<BaseEntity<TId>>
    {
        // TODO: Of the same type?!
        public bool Equals(BaseEntity<TId> other) => (other != null)
                                                     &&
                                                     (ReferenceEquals(this, other) ||
                                                      (!EqualityComparer<TId>.Default.Equals(Id, default(TId)) &&
                                                       EqualityComparer<TId>.Default.Equals(Id, other.Id)));

        [DataMember]
        public TId Id { get; protected set; }
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
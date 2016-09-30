// <copyright company="SIX Networks GmbH" file="DataModel.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using withSIX.Api.Models.Content.v3;

namespace withSIX.Play.Applications.DataModels
{
    public abstract class DataModelRequireId<TId> : DataModel, IHaveId<TId>
    {
        protected DataModelRequireId(TId id) {
            Contract.Requires<ArgumentOutOfRangeException>(!EqualityComparer<TId>.Default.Equals(id, default(TId)));

            Id = id;
        }

        public TId Id { get; }
    }
}
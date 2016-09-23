// <copyright company="SIX Networks GmbH" file="InMemoryDbSet.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using SN.withSIX.Core.Applications.Infrastructure;
using withSIX.Api.Models.Content.v3;

namespace SN.withSIX.Core.Infra.Services
{
    /*
     *  MOVE TO .PLAY!
    public interface ILocalDbSet<TEntity>
    {
        ReactiveList<TEntity> Local { get; }
    }

    public class InMemoryDbSet<TEntity, TId> : IDbSet<TEntity, TId>, ILocalDbSet<TEntity> where TEntity : IHaveId<TId>
    {
        readonly IQueryable<TEntity> _queryable;

        public InMemoryDbSet(ReactiveList<TEntity> collection) {
            _queryable = collection.AsQueryable();
            Local = collection;
        }

        public ReactiveList<TEntity> Local { get; }

        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator() => _queryable.GetEnumerator();

        public IEnumerator GetEnumerator() => _queryable.GetEnumerator();

        public Expression Expression => _queryable.Expression;
        public Type ElementType => _queryable.ElementType;
        public IQueryProvider Provider => _queryable.Provider;

        public TEntity Find(TId id) => _queryable.FirstOrDefault(x => EqualityComparer<TId>.Default.Equals(x.Id, id));
    }
    */
}
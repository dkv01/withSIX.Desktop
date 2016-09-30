// <copyright company="SIX Networks GmbH" file="IDbSet.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System.Linq;
using withSIX.Api.Models.Content.v3;
using withSIX.Api.Models.Exceptions;

namespace withSIX.Core.Applications.Infrastructure
{
    public interface IDbSet<out TEntity, in TId> : IQueryable<TEntity> where TEntity : IHaveId<TId>
    {
        TEntity Find(TId id);
    }

    public static class DbSetExtensions
    {
        public static TEntity FindOrThrow<TEntity, TId>(this IDbSet<TEntity, TId> dbSet, TId id)
            where TEntity : IHaveId<TId> {
            var item = dbSet.Find(id);
            if (item == null)
                throw new NotFoundException("Entity not found with key: " + id);
            return item;
        }
    }
}
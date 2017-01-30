// <copyright company="SIX Networks GmbH" file="QueryableExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using withSIX.Api.Models;
using withSIX.Api.Models.Extensions;

namespace withSIX.Play.Infra.Api
{
    public static class QueryableExtensions
    {
        public static PageModel<T> ToPageModel<T>(this IQueryable<T> query, int page, int pageSize) {
            if (!(page > 0)) throw new ArgumentOutOfRangeException("page > 0");
            if (!(pageSize > 0)) throw new ArgumentOutOfRangeException("pageSize > 0");

            var total = query.Count();
            var list = query.AddPaging(page*pageSize, pageSize).ToList();
            return new PageModel<T>(list, new PagingInfo(page, total, pageSize));
        }

        static IQueryable<T> AddPaging<T>(this IQueryable<T> query, int offSet, int pageSize) => query.Skip(offSet).Take(pageSize);
    }
}
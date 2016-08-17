// <copyright company="SIX Networks GmbH" file="AutoMapperExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using withSIX.Api.Models;
using withSIX.Api.Models.Extensions;

namespace SN.withSIX.Mini.Applications.Extensions
{
    public static class MappingExtensions
    {
        public static IMapper Mapper { get; set; }
        public static IConfigurationProvider Config => Mapper.ConfigurationProvider;
        public static Func<Type, Type> DetermineType { get; set; } = type => type;
        static Type GetType(object obj) => DetermineType(obj.GetType());

        public static IQueryable<T> ProjectTo<T>(this IQueryable expression) {
            Contract.Requires<ArgumentNullException>(expression != null);
            return expression.ProjectTo<T>(Config);
        }

        public static IQueryable<T> ProjectTo<T>(this IQueryable expression, IDictionary<string, object> parameters) {
            Contract.Requires<ArgumentNullException>(expression != null);
            return expression.ProjectTo<T>(Config, parameters);
        }

        public static void IgnoreAllMembers<TSource, TDestination>(
            this IMappingExpression<TSource, TDestination> expression) => expression.ForAllMembers(opt => opt.Ignore());

        public static void IgnoreAllOtherMembers<TSource, TDestination>(
            this IMappingExpression<TSource, TDestination> expression)
            => expression.ForAllOtherMembers(opt => opt.Ignore());

        public static async Task<TDesired> MapAsync<TSource, TDesired>(this Task<TSource> task, TDesired target)
            where TDesired : class {
            Contract.Requires<ArgumentNullException>(task != null);
            var r = await task.ConfigureAwait(false);
            return r.MapTo(target);
        }

        public static async Task<TDesired> MapAsync<TDesired>(this Task<object> task) {
            Contract.Requires<ArgumentNullException>(task != null);
            var r = await task.ConfigureAwait(false);
            return r.MapTo<TDesired>();
        }

        public static TDesired MapTo<TDesired>(this object input) => Mapper.Map<TDesired>(input);

        public static TDesired MapTo<TDesired>(this object input, Action<IMappingOperationOptions> opts)
            => Mapper.Map<TDesired>(input, opts);

        public static object MapTo(this object input, Type sourceType, Type destinationType) {
            Contract.Requires<ArgumentNullException>(input != null);
            return Mapper.Map(input, sourceType, destinationType);
        }

        public static object MapTo(this object input, object output, Type sourceType, Type destinationType) {
            Contract.Requires<ArgumentNullException>(input != null);
            Contract.Requires<ArgumentNullException>(output != null);
            return Mapper.Map(input, output, sourceType, destinationType);
        }

        public static object MapTo(this object input, Type sourceType, Type destinationType,
            Action<IMappingOperationOptions> opts) {
            Contract.Requires<ArgumentNullException>(input != null);
            return Mapper.Map(input, sourceType, destinationType, opts);
        }

        public static object MapTo(this object input, object output, Type sourceType, Type destinationType,
            Action<IMappingOperationOptions> opts) {
            Contract.Requires<ArgumentNullException>(input != null);
            Contract.Requires<ArgumentNullException>(output != null);
            return Mapper.Map(input, output, sourceType, destinationType, opts);
        }

        public static object MapTo(this object input, Type destinationType) {
            Contract.Requires<ArgumentNullException>(input != null);
            return Mapper.Map(input, destinationType);
        }

        public static object MapTo(this object input, Type destinationType, Action<IMappingOperationOptions> opts) {
            Contract.Requires<ArgumentNullException>(input != null);
            return Mapper.Map(input, destinationType, opts);
        }

        public static TDesired MapTo<TInput, TDesired>(this TInput input) => Mapper.Map<TInput, TDesired>(input);

        public static TDesired MapTo<TInput, TDesired>(this TInput input,
            Action<IMappingOperationOptions<TInput, TDesired>> opts) => Mapper.Map(input, opts);

        public static TDesired MapTo<TSource, TDesired>(this TSource input, TDesired output) where TDesired : class {
            Contract.Requires<ArgumentNullException>(output != null);
            return (TDesired) MapTo(input, output, GetType(input), GetType(output));
        }

        public static TDesired MapTo<TSource, TDesired>(this TSource input, TDesired output,
            Action<IMappingOperationOptions> options) where TDesired : class {
            Contract.Requires<ArgumentNullException>(output != null);
            return (TDesired) MapTo(input, output, GetType(input), GetType(output), options);
        }


        public static TDesired MapToOriginal<TSource, TDesired>(this TSource input, TDesired output)
            where TDesired : class {
            Contract.Requires<ArgumentNullException>(output != null);
            return Mapper.Map(input, output);
        }

        public static PageModel<T> ToPageModel<T>(this IEnumerable<T> items, int page, int perPage) {
            var count = items.Count();
            var i = items.Skip((page - 1) * perPage).Take(perPage);
            return i.ToPageModel(new PagingInfo(page, count, perPage));
        }

        public static PageModel<T> ToPageModelFromCtx<T>(this IEnumerable<T> e, ResolutionContext ctx2) {
            var c = (PagingContext)ctx2.Items["ctx"];
            return e.ToPageModel(c.Page, c.PageSize);
        }
    }


    public class PagingContext
    {
        public int Page { get; set; }
        public int PageSize { get; set; } = 24;
    }
}
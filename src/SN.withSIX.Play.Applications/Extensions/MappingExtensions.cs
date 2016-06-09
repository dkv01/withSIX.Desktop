// <copyright company="SIX Networks GmbH" file="MappingExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using AutoMapper;

namespace SN.withSIX.Play.Applications.Extensions
{
    public static class MappingExtensions
    {
        public static IMapper Mapper { get; set; }

        public static IMappingExpression<TSource, TDestination> IgnoreAllMembers<TSource, TDestination>(
            this IMappingExpression<TSource, TDestination> expression
            ) {
            expression.ForAllMembers(opt => opt.Ignore());
            return expression;
        }

        public static TDesired MapTo<TDesired>(this object input) => Mapper.Map<TDesired>(input);

        public static object MapTo(this object input, Type sourceType, Type destinationType) {
            Contract.Requires<ArgumentNullException>(input != null);
            return Mapper.Map(input, sourceType, destinationType);
        }

        public static object MapTo(this object input, object output, Type sourceType, Type destinationType) {
            Contract.Requires<ArgumentNullException>(input != null);
            Contract.Requires<ArgumentNullException>(output != null);
            return Mapper.Map(input, output, sourceType, destinationType);
        }

        public static object MapTo(this object input, Type destinationType) {
            Contract.Requires<ArgumentNullException>(input != null);
            return Mapper.Map(input, destinationType);
        }

        public static TDesired MapTo<TInput, TDesired>(this TInput input) => Mapper.Map<TInput, TDesired>(input);

        public static TDesired MapTo<TInput, TDesired>(this TInput input,
            Action<IMappingOperationOptions<TInput, TDesired>> opts) => Mapper.Map(input, opts);

        public static TDesired MapTo<TSource, TDesired>(this TSource input, TDesired output) where TDesired : class {
            Contract.Requires<ArgumentNullException>(output != null);
            return
                (TDesired)
                    MapTo(input, output, input.GetType(),
                        output.GetType()); // typeof(TSource), typeof(TDesired)
        }

        public static TDesired MapToOriginal<TSource, TDesired>(this TSource input, TDesired output)
            where TDesired : class {
            Contract.Requires<ArgumentNullException>(output != null);
            return Mapper.Map(input, output);
        }
    }
}
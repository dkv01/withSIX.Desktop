// <copyright company="SIX Networks GmbH" file="AutoMapperExtensions.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Diagnostics.Contracts;
using AutoMapper;

namespace SN.withSIX.Mini.Applications.Extensions
{
    public static class MappingExtensions
    {
        public static IMapper Mapper { get; set; }

        public static void IgnoreAllMembers<TSource, TDestination>(
            this IMappingExpression<TSource, TDestination> expression
            ) => expression.ForAllMembers(opt => opt.Ignore());

        public static void IgnoreAllOtherMembers<TSource, TDestination>(
            this IMappingExpression<TSource, TDestination> expression
            ) => expression.ForAllOtherMembers(opt => opt.Ignore());

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